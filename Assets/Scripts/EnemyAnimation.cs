using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyFSM))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [SerializeField, Min(0f)]
    float _blendTime = 0.15f;

    NavMeshAgent _agent;
    EnemyFSM _fsm;

    //매번 문자열을 반환하지 않도록 파마미터 ID를 저장
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int isTurningHash = Animator.StringToHash("IsTurning");
    static readonly int TurnDirectionHash = Animator.StringToHash("TurnDirection");
    static readonly int IsSearchingHash = Animator.StringToHash("IsSearching");
    //아주 작은 움직임은 정지로 처리
    const float StopThreshole = 0.05f;
     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  
        _fsm = GetComponent<EnemyFSM>();

        if( _animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if(_animator == null)
        {
            Debug.LogError("Animator를 연결하세요.", this);
            enabled = false;
            return;
        }

        //애니메이션이 루트 모션으로 위치를 변경하지 않도록 설정
        _animator.applyRootMotion = false;
    }

     void LateUpdate()
    {
        //게임이 일시정지된 동안 파라미터 갱신 중단
        if (Time.timeScale == 0f) return;

        bool fsmActive = _fsm.isActiveAndEnabled;
        bool isSearching = fsmActive && _fsm.IsSearchingAround;
        bool isTurning = fsmActive && !isSearching && _fsm.IsTurning;

        _animator.SetBool(IsSearchingHash, isSearching);

        _animator.SetBool(isTurningHash,isTurning);

        _animator.SetFloat(TurnDirectionHash, isTurning ? _fsm.TurnDirection : 0f);
        if (isSearching || isTurning)
        {
            _animator.SetFloat(SpeedHash, 0f);
        }
        else
        {
            _animator.SetFloat(SpeedHash, GetMovementSpeed(), _blendTime, Time.deltaTime);
        }
    }

    float GetMovementSpeed()
    {
        if (!_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return 0f;

        Vector3 velocity = _agent.velocity;

        velocity.y = 0f;

        float speed = velocity.magnitude;

        return speed < StopThreshole ? 0f : speed;
    }
}
