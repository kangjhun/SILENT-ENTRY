using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [SerializeField, Min(0f)]
    float _blendTime = 0.15f;

    NavMeshAgent _agent;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
     void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();  

        if( _agent == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if(_animator == null)
        {
            Debug.LogError("Animator를 연결하세요.", this);
            enabled = false;
            return;
        }

        _animator.applyRootMotion = false;
    }

     void LateUpdate()
    {
        if (Time.timeScale == 0f) return;

        float speed = 0f;

        if(_agent.enabled && _agent.isOnNavMesh && !_agent.isStopped)
        {
            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;

            speed = velocity.magnitude; 

            if(speed < 0.05f)
            {
                speed = 0f;
            }
        }

        _animator.SetFloat(SpeedHash,speed,_blendTime,Time.deltaTime);
    }
}
