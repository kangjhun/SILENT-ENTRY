using UnityEngine;

[RequireComponent (typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] Animator _animator;
    [Header("애니메이션 전환")]
    [SerializeField, Min(0f)] float _blendTime = 0.1f;

     PlayerMovement _playerMovement;

    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");
    static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
     void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();

        if (_animator == null)
        {
            Debug.LogError("animator를 연결해주세요", this);
            enabled = false;
            return;
        }

        _animator.applyRootMotion = false;
    }

    void LateUpdate()
    {
        if(Time.timeScale ==  0f) { return; }

        Vector2 move =_playerMovement.AnimationMove;

        _animator.SetFloat(MoveXHash,move.x,_blendTime,Time.deltaTime);
        _animator.SetFloat(MoveYHash,move.y,_blendTime,Time.deltaTime);
        _animator.SetBool(IsRunningHash,_playerMovement.IsRunning );
    }
}
