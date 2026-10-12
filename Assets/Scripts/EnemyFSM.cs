using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(NavMeshAgent),typeof(EnemyVision))]
public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,//정해진 지점을 순서대로 순찰
        Investigate,//소리가 발생한 위치로 이동
        Chase,//보이는 플레이어를 추격
        Search,//마지막 확인 위치로 이동한 뒤 주변 수색
        Return//순찰 지점을 복귀
    }

    [Header("현재 상태")]
    [SerializeField] EnemyState _currentState;

    [Header("순찰")]
    [SerializeField] Transform[] _patrolPoints;
    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _patrolWaitTime = 1f;

    [Header("순찰 출발 전 회전")]
    [SerializeField, Min(1f)] float _turnSpeed = 240f;
    [SerializeField, Range(1f, 45f)] float _startMoveAngle = 15f;

    bool _turningBeforeMove;

    [Header("조사")]
    [SerializeField, Min(0.1f)] float _investigateSpeed = 1.5f;

    [Header("추격")]
    [SerializeField] float _chaseSpeed = 4f;
    [SerializeField] float _repathInterval = 0.2f;

    [Header("수색")]
    [SerializeField] float _searchDuration = 4f;
    [SerializeField] float _searchLookAngle = 60f;

    [Header("이동 판정")]
    [SerializeField] float _stoppingDistance = 0.3f;
    [SerializeField] float _sampleRadius = 2f;
    [SerializeField] float _moveTimeout = 12f;

    [Header("플레이어 붙잡기")]
    [SerializeField] GameManager _gameManger;
    [SerializeField, Min(0.1f)] float _catchDistance = 1.2f;

    [Header("문 통과")]
    [SerializeField] DoorController[] _doors;
    [SerializeField, Min(0.1f)] float _doorLookAhead = 1.5f;

    NavMeshAgent _agent;
    EnemyVision _vision;
    NavMeshPath _path;

    int _patrolIndex;

    float _waitTimer; //순찰 지점 대기 시간
    float _moveTimer; //목적지로 이동한 시간
    float _searchTimer; //제자리에서 주변을 살핀 시간
    float _nextMoveTime; //다음 경로 갱신이 가능한 시각

    bool _hasDestination; //이동 경로 설정에 성공했는지
    bool _searchingAround; //수색 위치에 도착해 주변을 살피는 중인지
    bool _initialized; //초기 설정을 마쳤는지
    bool _waitingForDoor;

    Vector3 _noisePosition;
    Vector3 _lastSeenPosition;
    Vector3 _searchPosition;
    
    Quaternion _searchStartRotation;

    const float ArrivalTolerance = 0.1f;
    const float ReturnRetryInterval = 1f;

    public EnemyState CurrentState => _currentState;
    public bool IsTurning => _turningBeforeMove && !_waitingForDoor;
    public float TurnDirection {  get; private set; }   

    //Agent가 실제 NavMesh 이동을 수행할 수 있는 상태인지 확인
    bool AgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

    //GameManager를 연결하지 않은 AI 단독 테스트는 허용
    bool GameEnded => _gameManger != null && !_gameManger.IsPlaying;
     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  
        _vision = GetComponent<EnemyVision>();  
        _path = new NavMeshPath();  
    }

    void Start()
    {

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }
        

        _agent.stoppingDistance = _stoppingDistance;
        _agent.updateRotation = true;

        _initialized = true;
        _currentState = EnemyState.Patrol;
        //최초 상태는 ChangeState를 거치지 않고 직접 시작
        EnterState();
    }

    bool ValidateSetup()
    {
        if (!AgentReady)
        {
            Debug.LogError("NavMeshAgent와 적의 NavMesh 배치를 확인하시오.", this);
            return false;
        }

        if (_vision == null)
        {
            Debug.LogError("같은 오브젝트에 EnemyVision이 필요합니다.", this);
            return false;
        }
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            Debug.LogError("순찰 지점을 연결하시오.", this);
            return false;
        }

        foreach (Transform point in _patrolPoints)
        {
            if (point == null)
            {
                Debug.LogError("Patrol Poins에 비어 있는 항목이 있습니다.", this);
                return false;
            }
        }
        return true;
    }

    void Update()
    {
        if (!_initialized || !AgentReady) return;

        //상태 처리 중단뿐 아니라 Agent의 이동도 정지
        if (GameEnded)
        {
            if(_hasDestination || !_agent.isStopped)
            {
                StopMoving();
            }
            return;
        }

        //어떤 상태에서든 플레이어 발견을 최우선으로 처리
        UpdatePlayerDetection();
        if (UpdateDoorBeforeMove()) return;
        if (UpdateTurnBeforeMove()) return;

        switch(_currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Investigate:
                UpdateInvestigate();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Search:
                UpdateSearch();
                break;
            case EnemyState.Return:
                UpdateReturn();
                break;
        }
        if (!GameEnded)
        {
            UpdateDoorBeforeMove();
        }
    }

    void UpdatePlayerDetection()
    {
        bool seesPlayer = _vision.CanSeePlayer && _vision.PlayerTarget != null;

        if (seesPlayer)
        {
            //보이는 동안만 플레이어 위치를 기억
            _lastSeenPosition = _vision.PlayerTarget.position;
            ChangeState(EnemyState.Chase);
        }
        else if (_currentState == EnemyState.Chase)
        {
            //놓친 뒤에는 현재 플레이어 위치를 따라가지 않고 마지막으로 목격한 위치를 수색
            _searchPosition = _lastSeenPosition;
            ChangeState(EnemyState.Search);
        }
    }

    void ChangeState(EnemyState nextState)
    {
        //같은 상태를 다시 초기화하지 않도록 방지
        if(_currentState == nextState) return;
        Debug.Log($"{_currentState}-> {nextState}", this);
        _currentState = nextState;
        EnterState();
    }

    void EnterState()
    {
        _waitingForDoor = false;
        _turningBeforeMove = false;

        //이전 상태의 이동 경로와 타이머 초기화
        _agent.updateRotation = true;
        _agent.ResetPath();
        _agent.isStopped = false;

        _hasDestination = false;
        _searchingAround = false;

        _waitTimer = 0f;
        _moveTimer = 0f;
        _searchTimer = 0f;
        _nextMoveTime = 0f;

        //순찰, 수색, 복귀의 기본 이동 속도
        _agent.speed = _walkSpeed;

        switch(_currentState)
        {
            case EnemyState.Patrol:
                MoveTo(_patrolPoints[_patrolIndex].position,true);
                break;   
            case EnemyState.Return:
                SelectClosestPatrolPoint();
                MoveTo(_patrolPoints[_patrolIndex].position);
                break;
            case EnemyState.Investigate:
                _agent.speed = _investigateSpeed;
                MoveTo(_noisePosition);
                break;
            case EnemyState.Chase:
                _agent.speed = _chaseSpeed;
                //목적지는 UpdateChase에서 주기적으로 갱신
                break;
            case EnemyState.Search:
                MoveTo(_searchPosition);
                break;    
            
        }
    }

    void UpdatePatrol()
    {
        _moveTimer += Time.deltaTime;
        if(_hasDestination && !HasArrived())
        {
            //목적지로 이동 중이면 기다림
            if (_moveTimer < _moveTimeout) return;
            //너무 오래 걸리면 현재 경로를 포기
            StopMoving();
        }

        //도착했거나 경로를 포기한 뒤 잠시 대기
        _waitTimer += Time.deltaTime;

        if(_waitTimer < _patrolWaitTime) return;

        _waitTimer = 0f;
        _moveTimer = 0f;

        //마지막 지점 다음에는 첫 번째 지점으로 돌아감
        _patrolIndex = (_patrolIndex +1 ) % _patrolPoints.Length;

        MoveTo(_patrolPoints[_patrolIndex].position,true);
    }

    void UpdateInvestigate()
    {
        _moveTimer += Time.deltaTime;

        bool arrived = HasArrived();

        if(_hasDestination && !arrived && _moveTimer < _moveTimeout) return;

        //도착했다면 소음 위치를 수색, 이동실패나 시간 초과라면 현재 위치를 수색
        _searchPosition = arrived ? _noisePosition : transform.position;   
        ChangeState(EnemyState.Search);
    }

    void UpdateChase()
    {
        //붙잡기 판정은 경로 갱신 간격과 관계없이 매 프레임 실행
        if (CheckCatchPlayer()) return;

        //경로 계산은 일정 간격으로 제한
        if (Time.time < _nextMoveTime) return;
        _nextMoveTime = Time.time + _repathInterval;
        MoveTo(_lastSeenPosition);
    }

    bool CheckCatchPlayer()
    {
        if(_gameManger == null || !_gameManger.IsPlaying) return false;
        if(!_vision.CanSeePlayer || _vision.PlayerTarget == null) return false;
        
        //눈 위치에서 플레이어 탐지 기준점까지의 거리
        Vector3 origin = _vision.EyePosition;
        Vector3 toPlayer = _vision.PlayerTarget.position - origin;

        float distance = toPlayer.magnitude;

        if(distance > _catchDistance) return false;

        //가까워도 중간에 장애물이 있으면 붙잡지 못함
        if(distance > 0.001f && _vision.CheckObstacle(origin,toPlayer/distance,distance,out _))
        {
            return false;
        }

        StopMoving();
        _gameManger.FailMission();

        return true;
    }
    void UpdateSearch()
    {
        if (!_searchingAround)
        {
            _moveTimer += Time.deltaTime;

            //먼저 수색할 위치까지 이동
            if (_hasDestination && !HasArrived() && _moveTimer < _moveTimeout) return;
            StopMoving();

            //수색 중에는 코드가 직접 회전하므로 Agent 회전 중단
            _agent.updateRotation = false;

            _searchStartRotation = transform.rotation;
            _searchTimer = 0f;
            _searchingAround = true;    
        }

        _searchTimer += Time.deltaTime;

        float duration = Mathf.Max(_searchDuration, 0.01f);
        float progress = Mathf.Clamp01(_searchTimer / duration);

        //정면 -> 한쪽 -> 반대쪽 -> 정면 순서로 주변을 살핌
        float angle = Mathf.Sin(progress * Mathf.PI * 2f) * _searchLookAngle;

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _searchStartRotation;

        if(progress >=1f)
        {
            ChangeState(EnemyState.Return);
        }
    }

    void UpdateReturn()
    {
        if (HasArrived())
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        _moveTimer += Time.deltaTime;

        bool timeOut = _moveTimer >= _moveTimeout;

        bool invalidPath = !_agent.pathPending && _agent.pathStatus != NavMeshPathStatus.PathComplete;

        if(_hasDestination && (timeOut || invalidPath))
        {
            StopMoving();
        }

        //복귀 경로가 없으면 일정 간격으로 재시도
        if(!_hasDestination && Time.time >= _nextMoveTime)
        {
            _nextMoveTime = Time.time + ReturnRetryInterval;
            _moveTimer = 0f;

            MoveTo(_patrolPoints[_patrolIndex].position);
        }
    }

    void MoveTo(Vector3 position, bool turnBeforeMove = false)
    {
        //현재 Agent의 종류와 이동 가능한 영역에 맞춰 검색
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask,
        };

        //요청 위치 근처에서 NavMesh 위의 지점을 찾음
        bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, _sampleRadius, filter);
        //실제로 끝까지 도달 가능한 경로만 허용
        if(!found || !_agent.CalculatePath(hit.position,_path) || _path.status != NavMeshPathStatus.PathComplete)
        {
            StopMoving();
            return;
        }

        _agent.isStopped = false;
        _hasDestination = _agent.SetPath(_path);

        if (!_hasDestination) { StopMoving(); return; }

        _turningBeforeMove = turnBeforeMove;

        _agent.updateRotation = !turnBeforeMove;
        _agent.isStopped = turnBeforeMove;
    }

    bool HasArrived()
    {
        if(!_hasDestination || _agent.pathPending) return false;
        if(_agent.pathStatus != NavMeshPathStatus.PathComplete) return false;
        //정지 거리와 작은 오차 범위 안이면 도착으로 판단
        return _agent.remainingDistance <= _agent.stoppingDistance + ArrivalTolerance;
    }

    void StopMoving()
    {
        _waitingForDoor = false;
        _turningBeforeMove = false;
        _agent.updateRotation = true;

        _agent.ResetPath();
        _agent.isStopped = true;
        _hasDestination = false;
    }

    //라디오나 PlayerNoise에서 소음 발생 시 호출
    public void HearNoise(Vector3 position, float hearingRadius)
    {
        if(!_initialized || !isActiveAndEnabled || !AgentReady) return;
        if(GameEnded) return;
        //플레이어 추격이나 시각 탐지를 소음보다 우선
        if (_currentState == EnemyState.Chase || _vision.CanSeePlayer) return;
        if (hearingRadius <= 0f) return;

        float sqrDistance = (position - _vision.EyePosition).sqrMagnitude;
        if (sqrDistance > hearingRadius * hearingRadius)
        {
            return;
        }

        _noisePosition = position;  

        if(_currentState == EnemyState.Investigate)
        { //조사 중 새로운 소리를 들으면 목적지를 새 위치로 변경
            _moveTimer = 0f;
            MoveTo(_noisePosition);
        }
        else
        {
            ChangeState(EnemyState.Investigate);
        }
    }

    bool UpdateTurnBeforeMove()
    {
        if(!_turningBeforeMove) return false;
        if (_agent.pathPending) return true;

        if(!_hasDestination || !_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            StopMoving();
            return false;
        }

        Vector3 direction = _agent.steeringTarget - transform.position;
        direction.y = 0f;

        if(HasArrived() || direction.sqrMagnitude < 0.0001f)
        {
            _turningBeforeMove = false;
            _agent.updateRotation = true;
            _agent.isStopped = false;
            return false;
        }

        float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        TurnDirection = signedAngle < 0f ? -1f : 1f;

        float turnAmount = Mathf.Clamp(signedAngle, -_turnSpeed * Time.deltaTime, _turnSpeed * Time.deltaTime);

        transform.Rotate(Vector3.up, turnAmount, Space.World);

        float angle = Mathf.Abs(signedAngle - turnAmount);

        if(angle <= _startMoveAngle)
        {
            _turningBeforeMove = false;
            _agent.updateRotation = true;
            _agent.isStopped = false;
        }

        return true;
    }
    bool UpdateDoorBeforeMove()
    {
        DoorController door = FindDoorOnPath();
        if(door != null)
        {
            door.RequestOpen();
            if (!door.IsPassable)
            {
                _agent.isStopped = true;
                _waitingForDoor = true;
                return true;
            }
        }

        if (_waitingForDoor)
        {
            _waitingForDoor = false;

            if (_hasDestination && !_turningBeforeMove)
                _agent.isStopped = false;
        }
        return false;
    }

    DoorController FindDoorOnPath()
    {
        if(!_hasDestination || !_agent.hasPath || _agent.pathPending || HasArrived()||_doors == null)
            return null;

        Vector3[] corners = _agent.path.corners;

        if(corners.Length < 2) return null;

        Vector3 start = _agent.nextPosition;

        Vector3 heightOffset = Vector3.up * (_agent.height * 0.5f);

        float remaining = Mathf.Max(_doorLookAhead, _agent.radius + _agent.speed * Time.deltaTime + 0.2f);

        for(int i=1; i<corners.Length && remaining > 0f; i++)
        {
            Vector3 direction = corners[i] -start;
            float length = direction.magnitude;

            if(length < 0.0001f)
            {
                start = corners[i];
                continue;
            }

            float segmentLength = Mathf.Min(length,remaining);
            Vector3 end = start + direction / length * segmentLength;

            DoorController nearest = null;
            float nearestFraction = float.PositiveInfinity;

            foreach(DoorController door in _doors)
            {
                if (door == null) continue;

                bool intersects = door.IntersectsSegment(start + heightOffset, end + heightOffset, _agent.radius, out float fraction);
                if(!intersects) continue;

                if (door.IsPassable)
                {
                    door.RequestOpen();
                    continue;
                }

                if(fraction < nearestFraction)
                {
                    nearestFraction = fraction;
                    nearest = door;
                }
            }

            if(nearest != null) return nearest;

            remaining -= segmentLength;
            start = end;
        }
        return null;
    }

    void SelectClosestPatrolPoint()
    {
        float shortestDistance = Mathf.Infinity;
        int closestIndex = _patrolIndex;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };

        for(int i = 0; i< _patrolPoints.Length; i++)
        {
            if (!NavMesh.SamplePosition(_patrolPoints[i].position,out NavMeshHit hit, _sampleRadius, filter))
            {
                continue;
            }

            if (!_agent.CalculatePath(hit.position, _path)) continue;

            if(_path.status != NavMeshPathStatus.PathComplete) continue;

            Vector3[] corners = _path.corners;
            if (corners.Length == 0) continue;
            float distance = 0f;

            for(int j = 1; j < corners.Length; j++)
            {
                distance += Vector3.Distance(corners[j - 1], corners[j]);
            }

            if(distance < shortestDistance)
            {
                shortestDistance = distance;
                closestIndex = i;
            }
        }

        _patrolIndex = closestIndex;    
    }
}
