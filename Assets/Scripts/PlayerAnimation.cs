using UnityEngine;

[RequireComponent (typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] Animator _animator;
    [Header("애니메이션 전환")]
    [SerializeField, Min(0f)] float _blendTime = 0.1f;

     PlayerMovement _playerMovement;
     PlayerCover _playerCover;

    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");
    static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    static readonly int CrouchSpeedHash = Animator.StringToHash("CrouchSpeed");
    static readonly int ISInCoverHash = Animator.StringToHash("IsInCover");
    static readonly int CoverMoveHash = Animator.StringToHash("CoverMove");
     void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerCover = GetComponent<PlayerCover>();

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
        bool isCrouching = _playerMovement.IsCrouching;
        bool isInCover = _playerCover != null && _playerCover.IsInCover;

        _animator.SetFloat(MoveXHash,move.x,_blendTime,Time.deltaTime);
        _animator.SetFloat(MoveYHash,move.y,_blendTime,Time.deltaTime);
        _animator.SetBool(IsRunningHash,_playerMovement.IsRunning && !isCrouching );

        _animator.SetBool(IsCrouchingHash,isCrouching); 
        _animator.SetBool(ISInCoverHash,isInCover);
        float crouchSpeed = Mathf.Clamp01(move.magnitude);

        _animator.SetFloat(CrouchSpeedHash, crouchSpeed, _blendTime, Time.deltaTime);
        _animator.SetFloat(CoverMoveHash, _playerMovement.CoverMove, _blendTime, Time.deltaTime);
    }
}
