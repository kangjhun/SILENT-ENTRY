using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyFSM))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [SerializeField, Min(0f)]
    float _blendTime = 0.15f;//이동 애니메이션 전환 시간

    NavMeshAgent _agent;
    EnemyFSM _fsm;

    //문자열을 매번 해시로 변환하지 않도록 파라미터 ID 저장
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

        //위치 이동은 NavMeshAgent가 담당
        _animator.applyRootMotion = false;
    }

     void LateUpdate()
    {
        //일시정지 중에는 파라미터 갱신 중단
        if (Time.timeScale == 0f) return;
        //FSM에서 탐색과 회전 상태 확인
        bool fsmActive = _fsm.isActiveAndEnabled;
        bool isSearching = fsmActive && _fsm.IsSearchingAround;
        bool isTurning = fsmActive && !isSearching && _fsm.IsTurning;

        _animator.SetBool(IsSearchingHash, isSearching);

        _animator.SetBool(isTurningHash,isTurning);

        _animator.SetFloat(TurnDirectionHash, isTurning ? _fsm.TurnDirection : 0f);
        //탐색하거나 제자리 회전 중이면 이동 애니메이션 정지
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
        //이동할 수 없는 상태이면 속도 0 반환
        if (!_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return 0f;
        //수직 이동을 제외한 실제 이동 속도 계산
        Vector3 velocity = _agent.velocity;

        velocity.y = 0f;

        float speed = velocity.magnitude;

        return speed < StopThreshole ? 0f : speed;
    }
}
