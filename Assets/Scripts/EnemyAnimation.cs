using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [SerializeField, Min(0f)]
    float _blendTime = 0.15f;

    NavMeshAgent _agent;

    //매번 문자열을 반환하지 않도록 파마미터 ID를 저장
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    //아주 작은 움직임은 정지로 처리
    const float StopThreshole = 0.05f;
     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  

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

        float speed = GetMovementSpeed();


        //실제 이동 속도를 Blend Tree에 전달
        //blendTime을 사용해 속도 값이 부드럽게 바뀌도록 처리
        _animator.SetFloat(SpeedHash,speed,_blendTime,Time.deltaTime);
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
