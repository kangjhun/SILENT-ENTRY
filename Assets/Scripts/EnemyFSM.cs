using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(NavMeshAgent),typeof(EnemyVision))]
public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Investigate,
        Chase,
        Search,
        Return
    }

    [SerializeField] EnemyState _currentState;

    [SerializeField] Transform[] _patrolPoints;
    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _patrolWaitTime = 1f;

    [SerializeField] float _chaseSpeed = 4f;
    [SerializeField] float _repathInterval = 0.2f;

    [SerializeField] float _searchDuration = 4f;
    [SerializeField] float _searchLookAngle = 60f;
    Quaternion _searchStartRotation;

    [SerializeField] float _stoppingDistance = 0.3f;
    [SerializeField] float _sampleRadius = 2f;
    [SerializeField] float _moveTimeout = 12f;

    NavMeshAgent _agent;
    EnemyVision _vision;
    NavMeshPath _path;

    int _patrolIndex;

    float _waitTimer;
    float _moveTimer;
    float _searchTimer;
    float _nextMoveTime;

    bool _hasDestination;
    bool _searchingAround;
    bool _initialized;

    Vector3 _noisePosition;
    Vector3 _lastSeenPosition;
    Vector3 _searchPosition;

    public EnemyState CurrentState => _currentState;

    bool AgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  
        _vision = GetComponent<EnemyVision>();  
        _path = new NavMeshPath();  
    }

    void Start()
    {
        if (!AgentReady)
        {
            Debug.LogError("NavMeshAgent와 적의 NavMesh 배치를 확인하시오.", this);
            enabled = false;
            return;
        }

        if(_vision == null)
        {
            Debug.LogError("같은 오브젝트에 EnemyVision이 필요합니다.", this);
            enabled = false;
            return;
        }
        if(_patrolPoints == null || _patrolPoints.Length == 0)
        {
            Debug.LogError("순찰 지점을 연결하시오.", this);
            enabled = false;
            return;
        }

        foreach(Transform point in _patrolPoints)
        {
            if(point == null)
            {
                Debug.LogError("Patrol Poins에 비어 있는 항목이 있습니다.", this);
                enabled = false;
                return;
            }
        }

        _agent.stoppingDistance = _stoppingDistance;
        _agent.updateRotation = true;

        _initialized = true;
        _currentState = EnemyState.Patrol;
        EnterState();
    }


    void Update()
    {
        if (!_initialized || !AgentReady) return;

        bool seesPlayer = _vision.CanSeePlayer && _vision.PlayerTarget != null;

        if (seesPlayer)
        {
            _lastSeenPosition = _vision.PlayerTarget.position;
            ChangeState(EnemyState.Chase);
        }else if(_currentState == EnemyState.Chase)
        {
            _searchPosition = _lastSeenPosition;
            ChangeState(EnemyState.Search); 
        }

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
    }

    void ChangeState(EnemyState nextState)
    {
        if(_currentState == nextState) return;
        Debug.Log($"{_currentState}-> {nextState}", this);
        _currentState = nextState;
        EnterState();
    }

    void EnterState()
    {
        _agent.updateRotation = true;
        _agent.ResetPath();
        _agent.isStopped = false;
        _hasDestination = false;
        _waitTimer = 0f;
        _moveTimer = 0f;
        _searchTimer = 0f;
        _nextMoveTime = 0f;
        _searchingAround = false;

        _agent.speed = _currentState == EnemyState.Chase ? _chaseSpeed : _walkSpeed;

        switch(_currentState)
        {
            case EnemyState.Patrol:
                MoveTo(_patrolPoints[_patrolIndex].position);
                break;
            case EnemyState.Investigate:
                MoveTo(_noisePosition);
                break;
            case EnemyState.Chase:

                break;
            case EnemyState.Search:
                MoveTo(_searchPosition);
                break;    
            case EnemyState.Return:
                MoveTo(_patrolPoints[_patrolIndex].position);
                break    ;    
        }
    }

    void UpdatePatrol()
    {
        _moveTimer += Time.deltaTime;
        if(_hasDestination && !HasArrived())
        {
            if (_moveTimer < _moveTimeout) return;
            StopMoving();
        }

        _waitTimer += Time.deltaTime;

        if(_waitTimer < _patrolWaitTime) return;

        _waitTimer = 0f;
        _moveTimer = 0f;

        _patrolIndex = (_patrolIndex +1 ) % _patrolPoints.Length;

        MoveTo(_patrolPoints[_patrolIndex].position);
    }

    void UpdateInvestigate()
    {
        _moveTimer += Time.deltaTime;
        if(_hasDestination && !HasArrived() && _moveTimer < _moveTimeout) return;

        _searchPosition = HasArrived() ? _noisePosition : transform.position;   
        ChangeState(EnemyState.Search);
    }

    void UpdateChase()
    {
        if (Time.time < _nextMoveTime) return;
        _nextMoveTime = Time.time + _repathInterval;
        MoveTo(_lastSeenPosition);
    }

    void UpdateSearch()
    {
        if (!_searchingAround)
        {
            _moveTimer += Time.deltaTime;

            if (_hasDestination && !HasArrived() && _moveTimer < _moveTimeout) return;
            StopMoving();

            _agent.updateRotation = false;

            _searchStartRotation = transform.rotation;
            _searchTimer = 0f;
            _searchingAround = true;    
        }

        _searchTimer += Time.deltaTime;

        float duration = Mathf.Max(_searchDuration, 0.01f);
        float progress = Mathf.Clamp01(_searchTimer / duration);

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

        if(_hasDestination && (_moveTimer >= _moveTimeout || (!_agent.pathPending && _agent.pathStatus != NavMeshPathStatus.PathComplete) ))
        {
            StopMoving();
        }

        if(!_hasDestination && Time.time >= _nextMoveTime)
        {
            _nextMoveTime = Time.time + 1f;
            _moveTimer = 0f;

            MoveTo(_patrolPoints[_patrolIndex].position);
        }
    }

    void MoveTo(Vector3 position)
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask,
        };

        bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, _sampleRadius, filter);
        if(!found || !_agent.CalculatePath(hit.position,_path) || _path.status != NavMeshPathStatus.PathComplete)
        {
            StopMoving();
            return;
        }

        _agent.isStopped = false;
        _hasDestination = _agent.SetPath(_path);

        if (!_hasDestination) { StopMoving(); }
    }

    bool HasArrived()
    {
        if(!_hasDestination || _agent.pathPending) return false;
        if(_agent.pathStatus != NavMeshPathStatus.PathComplete) return false;
        return _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
    }

    void StopMoving()
    {
        _agent.ResetPath();
        _agent.isStopped = true;
        _hasDestination = false;
    }

    public void HearNoise(Vector3 position, float hearingRadius)
    {
        if(!_initialized || !isActiveAndEnabled || !AgentReady) return;
        if (_currentState == EnemyState.Chase || _vision.CanSeePlayer) return;
        if (hearingRadius <= 0f) return;

        float sqrDistance = (position - _vision.EyePosition).sqrMagnitude;
        if (sqrDistance > hearingRadius * hearingRadius)
        {
            return;
        }

        _noisePosition = position;  

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
}
