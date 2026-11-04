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
        Return,//순찰 지점을 복귀
        Suspicious //플레이어를 발견하기 전 감지 중
    }

    [Header("현재 상태")]
    [SerializeField] EnemyState _currentState;

    [Header("순찰")]
    [SerializeField] Transform[] _patrolPoints;
    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _patrolWaitTime = 1f;

    [Header("목표물 획득 후 순찰")]
    [SerializeField] Transform[] _alertPatrolPoints;
    bool _alertPatrolRequested;
    bool _pendingPatrolChange;

    [Header("순찰 출발 전 회전")]
    [SerializeField, Min(1f)] float _turnSpeed = 240f;
    [SerializeField, Range(1f, 45f)] float _startMoveAngle = 15f;

    bool _turningBeforeMove;

    [Header("조사")]
    [SerializeField, Min(0.1f)] float _investigateSpeed = 2.8f;

    [Header("추격")]
    [SerializeField] float _chaseSpeed = 4f;
    [SerializeField] float _repathInterval = 0.2f;

    [Header("수색")]
    [SerializeField] float _searchDuration = 6.5f;

    [Header("이동 판정")]
    [SerializeField] float _stoppingDistance = 0.3f;
    [SerializeField] float _sampleRadius = 2f;
    [SerializeField] float _moveTimeout = 12f;

    [Header("플레이어 붙잡기")]
    [SerializeField] GameManager _gameManger;
    [SerializeField, Min(0.1f)] float _catchDistance = 1.2f;

    [Header("접촉 감지")]
    [SerializeField, Min(0f)] float _contactPadding = 0.05f;
    [SerializeField, Min(0.1f)] float _contactCatchDelay = 0.35f;


    [Header("문 통과")]
    [SerializeField] DoorController[] _doors;
    [SerializeField, Min(0.1f)] float _doorLookAhead = 1.5f;

    [Header("플레이어 감지")]
    [SerializeField, Min(0.1f)] float _detectionTime = 0.8f;
    [SerializeField, Min(0.1f)] float _detectionDecayTime = 1f;

    NavMeshAgent _agent;
    EnemyVision _vision;
    NavMeshPath _path;

    int _patrolIndex;
    int _activeNoisePriority;

    float _waitTimer; //순찰 지점 대기 시간
    float _moveTimer; //목적지로 이동한 시간
    float _searchTimer; //제자리에서 주변을 살핀 시간
    float _nextMoveTime; //다음 경로 갱신이 가능한 시각
    float _detectionAmount;
    float _lastNoiseHeardTime = float.NegativeInfinity; //마지막으로 유효한 소음을 들은 시간
    float _contactStartedAt;

    bool _hasDestination; //이동 경로 설정에 성공했는지
    bool _searchingAround; //수색 위치에 도착해 주변을 살피는 중인지
    public bool IsSearchingAround => _searchingAround;
    bool _initialized; //초기 설정을 마쳤는지
    bool _waitingForDoor;
    bool _touchingPlayer;

    bool _hasConfirmedPlayer; //이미 발견이 확정된 플레이어를 수색 중인지
    public float DetectionAmount => _detectionAmount;

    EnemyState _stateBeforeSuspicion; //감지를 시작하기 전에 하던 행동


    Vector3 _noisePosition;
    Vector3 _lastSeenPosition;
    Vector3 _searchPosition;

    const float ArrivalTolerance = 0.1f;
    const float ReturnRetryInterval = 1f;
    const float NoiseMemoryTime = 1f; //라디오 전달 간격(0.5초보다) 여유 있게 설정
    CharacterController _playerController;

    public EnemyState CurrentState => _currentState;
    public bool IsTurning => !_waitingForDoor && (_turningBeforeMove || (_currentState == EnemyState.Suspicious && Mathf.Abs(TurnDirection) > 0.01f));

    public float TurnDirection {  get; private set; }   

    //Agent가 실제 NavMesh 이동을 수행할 수 있는 상태인지 확인
    bool AgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

    //GameManager를 연결하지 않은 AI 단독 테스트는 허용
    bool GameEnded => _gameManger != null && !_gameManger.IsPlaying;

    //이동과 시야 감지에 필요한 컴포넌트 준비
     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  
        _vision = GetComponent<EnemyVision>();  
        _path = new NavMeshPath();  
    }
    //설정을 확인하고 순찰 시작
    void Start()
    {

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }
        

        _agent.stoppingDistance = _stoppingDistance;
        _agent.updateRotation = true;

        if(_vision.PlayerTarget != null)
        {
            _playerController = _vision.PlayerTarget.GetComponentInParent<CharacterController>();
        }

        if(_playerController == null)
        {
            Debug.LogWarning("접촉 감지 : PlayerTarget을 플레이어 CharacterController의 " + "오브젝트 또는 자식에 연결하세요.", this);
        }
        _initialized = true;
        _currentState = EnemyState.Patrol;
        //최초 상태는 ChangeState를 거치지 않고 직접 시작
        EnterState();
    }
    //적 AI 실행에 필요한 설정이 되어 있는지 확인
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
    //플레이어 감지, 문 학인 후 상태에 맞는 행동을 실행
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

        if(Time.timeScale <=0f) { return; }

        //어떤 상태에서든 플레이어 발견을 최우선으로 처리
        UpdatePlayerDetection();
        //순찰 또는 복귀 중이면 새 순찰 경로 적용
        if(_pendingPatrolChange && (_currentState == EnemyState.Patrol || _currentState == EnemyState.Return))
        { EnterState(); }    
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
            case EnemyState.Suspicious:
                UpdateSuspicious();
                break;
        }
        if (!GameEnded)
        {
            UpdateDoorBeforeMove();
        }
    }
    //현재 행동을 유지하면서 순찰 변경을 예약
    public void RequestAlertPatrol()
    {
        if(GameEnded || _alertPatrolRequested) return;
        //새 순찰 지점이 연결되어 있는지 확인
        if(_alertPatrolPoints == null || _alertPatrolPoints.Length == 0)
        {
            Debug.LogWarning("경계 강화 후 사용할 순찰 지점을 연결하세요.", this);
            return;
        }
        foreach(Transform point in _alertPatrolPoints)
        {
            if(point == null)
            {
                Debug.LogWarning("새 순찰 지점에 비어 있는 항목이 있습니다.", this);
                return;
            }
            _alertPatrolRequested = true;
            _pendingPatrolChange = true;
        }
    }

    //플레이어를 보면 감지량을 올리고, 놓치면 수색으로 전환
    void UpdatePlayerDetection()
    {
        bool touchingNow = IsTouchingPlayer();
        if (touchingNow && !_touchingPlayer)
            _contactStartedAt = Time.time;
        _touchingPlayer = touchingNow;
        if (_touchingPlayer)
        {
            _lastSeenPosition = _playerController.transform.position;
            _hasConfirmedPlayer = true;
            _detectionAmount = 1f;

            ChangeState(EnemyState.Chase);
            return;
        }

        bool seesPlayer = _vision.isActiveAndEnabled && _vision.CanSeePlayer && _vision.PlayerTarget != null;

        if(_currentState == EnemyState.Chase)
        {
            if(seesPlayer)
            {
                _lastSeenPosition = _vision.PlayerTarget.position;
            }
            else
            {
                _searchPosition = _lastSeenPosition;
                ChangeState(EnemyState.Search);
            }
            return;
        }
        if (seesPlayer)
        {
            //보이는 동안만 플레이어 위치를 기억
            _lastSeenPosition = _vision.PlayerTarget.position;
            if (_hasConfirmedPlayer)
            {
                _detectionAmount = 1f;
                ChangeState(EnemyState.Chase);
                return;
            }

            if (_currentState != EnemyState.Suspicious)
            {
                _stateBeforeSuspicion = _currentState;
                ChangeState(EnemyState.Suspicious); 
            }

            _detectionAmount = Mathf.MoveTowards(_detectionAmount, 1f, Time.deltaTime / Mathf.Max(_detectionTime, 0.1f));

            if(_detectionAmount >= 1f)
            {
                ChangeState(EnemyState.Chase);
            }

        }
        else
        {
            _detectionAmount = Mathf.MoveTowards(_detectionAmount, 0f, Time.deltaTime / Mathf.Max(_detectionDecayTime, 0.1f));

            if(_currentState == EnemyState.Suspicious && _detectionAmount <= 0f)
            {
                ChangeState(_stateBeforeSuspicion);
            }
        }
    }
    //현재 행동을 다른 상태로 변경
    void ChangeState(EnemyState nextState)
    {
        //같은 상태를 다시 초기화하지 않도록 방지
        if(_currentState == nextState) return;
        if (_searchingAround)
        {
            _searchingAround = false;
            _searchTimer = 0f;
            _agent.updateRotation = true;
        }
        _currentState = nextState;
        EnterState();
    }
    //새로운 상태에 맞게 이동 속도와 목적지 설정
    void EnterState()
    {
        //조사와 수색을 마치고 복귀할 때도 여기서 변경 적용
        if (_pendingPatrolChange && (_currentState == EnemyState.Patrol || _currentState == EnemyState.Return))
        {
            _patrolPoints = _alertPatrolPoints;
            _patrolIndex = 0;
            _pendingPatrolChange = false;

            //새 경로에서 가장 가까운 순찰 지점으로 복귀
            _currentState = EnemyState.Return;
        }

        _waitingForDoor = false;
        _turningBeforeMove = false;

        TurnDirection = 0f;
        if(_currentState == EnemyState.Patrol || _currentState == EnemyState.Return)
        {
            _hasConfirmedPlayer = false;
            _detectionAmount = 0f;

            _activeNoisePriority = 0;
            _lastNoiseHeardTime = float.NegativeInfinity;
        }

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
                _hasConfirmedPlayer = true;
                _detectionAmount = 1f;
                _agent.speed = _chaseSpeed;
                //목적지는 UpdateChase에서 주기적으로 갱신
                break;
            case EnemyState.Search:
                _agent.speed = _hasConfirmedPlayer ? _chaseSpeed : _walkSpeed;
                MoveTo(_searchPosition);
                break;
            case EnemyState.Suspicious:
                StopMoving();
                _agent.updateRotation = false;
                break;
            
        }
    }
    //순찰 지점에 도착하면 잠시 기다렸다가 다음 지점으로 이동
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
    //소리가 난 곳으로 이동한 뒤 주변 수색
    void UpdateInvestigate()
    {
        _moveTimer += Time.deltaTime;

        bool arrived = HasArrived();

        if(_hasDestination && !arrived && _moveTimer < _moveTimeout) return;

        //도착했다면 소음 위치를 수색, 이동실패나 시간 초과라면 현재 위치를 수색
        _searchPosition = arrived ? _noisePosition : transform.position;   
        ChangeState(EnemyState.Search);
    }
    //플레이어를 붙잡을 수 있는지 확인하고 추격 경로 갱신
    void UpdateChase()
    {
        //붙잡기 판정은 경로 갱신 간격과 관계없이 매 프레임 실행
        if (CheckCatchPlayer()) return;

        //경로 계산은 일정 간격으로 제한
        if (Time.time < _nextMoveTime) return;
        _nextMoveTime = Time.time + _repathInterval;
        MoveTo(_lastSeenPosition);
    }
    //플레이어와 접촉하거나 가까이 있으면 미션 실패 처리
    bool CheckCatchPlayer()
    {
        if(_gameManger == null || !_gameManger.IsPlaying) return false;
        if (_touchingPlayer)
        {
            if(Time.time - _contactStartedAt < _contactCatchDelay) return false;

            StopMoving();
            _gameManger.FailMission();
            return true;
        }
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
    //수색 위치로 이동한 뒤 일정 시간 주변을 살핌
    void UpdateSearch()
    {
        if (!_searchingAround)
        {
            _moveTimer += Time.deltaTime;

            //먼저 수색할 위치까지 이동
            if (_hasDestination && !HasArrived() && _moveTimer < _moveTimeout) return;
            StopMoving();

            //제자리 수색 중에는 Agent의 자동 회전 중단
            _agent.updateRotation = false;

            
            _searchTimer = 0f;
            _searchingAround = true; 

        }

        _searchTimer += Time.deltaTime;

        if (_searchTimer < Mathf.Max(_searchDuration, 0.01f)) return;
        //소음 조사 중이고 아직 소리가 들리면 현장에서 대기
        bool stillHearingNoise = !_hasConfirmedPlayer && Time.time - _lastNoiseHeardTime < NoiseMemoryTime;

        if (stillHearingNoise) return;

        ChangeState(EnemyState.Return);
        
    }
    //순찰 지점으로 복귀하고, 이동에 실패하면 다시 시도
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
    //감지 중인 플레이어 쪽으로 몸을 회전
    void UpdateSuspicious()
    {
        Vector3 direction = _lastSeenPosition - transform.position;
        direction.y = 0f;

        if(direction.sqrMagnitude < 0.001f)
        {
            TurnDirection = 0f;
            return;
        }

        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        TurnDirection = Mathf.Abs(angle) > 2f ? (angle < 0f ? -1f : 1f) : 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);

    }
    //목적지까지 이동 가능한 경로를 찾아 이동 시작
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
    //목적지에 도착했는지 확인
    bool HasArrived()
    {
        if(!_hasDestination || _agent.pathPending) return false;
        if(_agent.pathStatus != NavMeshPathStatus.PathComplete) return false;
        //정지 거리와 작은 오차 범위 안이면 도착으로 판단
        return _agent.remainingDistance <= _agent.stoppingDistance + ArrivalTolerance;
    }
    //이동을 멈추고 기존 경로 제거
    void StopMoving()
    {
        _waitingForDoor = false;
        _turningBeforeMove = false;
        _agent.updateRotation = true;

        _agent.ResetPath();
        _agent.isStopped = true;
        _hasDestination = false;
    }

    //들리는 범위와 소음 우선순위를 확인하고 소음 조사 시작
    public void HearNoise(Vector3 position, float hearingRadius,int priority =2)
    {
        
        if (!_initialized || !isActiveAndEnabled || !AgentReady) return;
        if (GameEnded || Time.timeScale <= 0f) return;
        //플레이어 추격이나 시각 탐지를 소음보다 우선
        if (_currentState == EnemyState.Chase || _vision.CanSeePlayer || _currentState == EnemyState.Suspicious ) return;
        if (_currentState == EnemyState.Search && _hasConfirmedPlayer) return;
        if (hearingRadius <= 0f) return;

        Vector3 toNoise = position - transform.position;
        toNoise.y = 0f;
        if (toNoise.sqrMagnitude > hearingRadius * hearingRadius) return;

        bool investingating = _currentState == EnemyState.Investigate || _currentState == EnemyState.Search;
        //이미 조사 중인 소리보다 우선순위가 낮으면 무시
        if (investingating && priority < _activeNoisePriority) return;

        Vector3 difference = position - _noisePosition;
        difference.y = 0f;

        bool samePosition = difference.sqrMagnitude <= 0.5f * 0.5f;

        _activeNoisePriority = priority;
        _lastNoiseHeardTime = Time.time;//이동하거나 수색 중이어도 소음이 계속 들리는 정보는 갱신
        //같은 위치의 소리는 들린 시간만 갱신하고 이동은 다시 시작하지 않음
        if (investingating && samePosition) return;

        _noisePosition = position;
        _hasConfirmedPlayer = false;


        if(_currentState == EnemyState.Investigate)
        {
            _moveTimer = 0f;
            MoveTo(_noisePosition);
        }
        else
        {
            ChangeState(EnemyState.Investigate);
        }
    }
    //출발 전에 이동할 방향을 바라보도록 회전
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
    //앞에 문이 있으면 열고, 완전히 열릴 때까지 대기
    bool UpdateDoorBeforeMove()
    {
        DoorController door = FindDoorOnPath();
        if(door != null)
        {
            door.RequestOpen(transform.position);
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
    //앞으로 이동할 경롱에서 통과해야 할 문을 찾음
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
                    door.RequestOpen(transform.position);
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
    //실제 이동 경로가 가장 짧은 순찰 지점을 복귀 위치로 선택
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
    //플레이어와 몸이 닿을 만큼 가까운지 확인하고 벽 너머는 제외
    bool IsTouchingPlayer()
    {
        if (_playerController == null || !_playerController.enabled || !_playerController.gameObject.activeInHierarchy) return false;
        Bounds playerBounds = _playerController.bounds;
        float enemyBottom = transform.position.y + _agent.baseOffset;
        float enemyTop = enemyBottom + _agent.height;

        if (playerBounds.max.y < enemyBottom || playerBounds.min.y > enemyTop) return false;
        Vector3 toPlayer = playerBounds.center - transform.position;
        toPlayer.y = 0f;

        float playerRadius = Mathf.Max(playerBounds.extents.x, playerBounds.extents.z);
        float contactDistance = _agent.radius + playerRadius + _contactPadding;

        if (toPlayer.sqrMagnitude > contactDistance * contactDistance) return false;

        Vector3 direction = playerBounds.center - _vision.EyePosition;
        float distance = direction.magnitude;

        if (distance > 0.001f && _vision.CheckObstacle(_vision.EyePosition, direction / distance, distance, out _)) return false;
        return true;
    }
}
