using UnityEngine;
using UnityEngine.AI;
//추격 중 발이 바닥에 닿을 때 발소리 재생
[RequireComponent(typeof(AudioSource))]
public class EnemyFootstepAudio : MonoBehaviour
{
    [SerializeField] EnemyFSM _fsm;
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _footstepClips;

    [SerializeField, Range(0f, 1f)] float _volume = 0.5f;


    void Awake()
    {
        if(_fsm == null)
            _fsm = GetComponent<EnemyFSM>();
        if(_agent == null)
            _agent = GetComponent<NavMeshAgent>();
        if(_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
    }

    //달리기 애니메이션에서 발이 닿는 순간 호출
    public void PlayFootstep()
    {
        if (!isActiveAndEnabled || Time.timeScale == 0f) return;
        if(_fsm==null || !_fsm.isActiveAndEnabled || !_fsm.IsChasing) return;
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return; 

        //실제로 이동하지 않으면 소리 생략
        Vector3 velocity = _agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude < 0.05f * 0.05f) return;
        if (_audioSource == null || !_audioSource.isActiveAndEnabled || _footstepClips == null ) return;

        _audioSource.PlayOneShot(_footstepClips, _volume);
    }
}
