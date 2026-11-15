using UnityEngine;
//플레이어의 이동 상태를 애니메이션에 반영
[RequireComponent (typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] Animator _animator;
    [Header("애니메이션 전환")]
    [SerializeField, Min(0f)] float _blendTime = 0.1f;

     PlayerMovement _playerMovement;
     PlayerCover _playerCover;
    //애니메이터 파라미터 ID 저장
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");
    static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    static readonly int CrouchSpeedHash = Animator.StringToHash("CrouchSpeed");
    static readonly int ISInCoverHash = Animator.StringToHash("IsInCover");
    static readonly int CoverSideHash = Animator.StringToHash("CoverSide");
    static readonly int CoverSpeedHash = Animator.StringToHash("CoverSpeed");
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
        //애니메이션이 플레이어 위치를 직접 바꾸지 않도록 설정
        _animator.applyRootMotion = false;
    }

    void LateUpdate()
    {
        if(Time.timeScale ==  0f) { return; }
        //현재 이동 방향과 앉기, 엄폐 상태 확인
        Vector2 move =_playerMovement.AnimationMove;
        bool isCrouching = _playerMovement.IsCrouching;
        bool isInCover = _playerCover != null && _playerCover.IsInCover;
        //이동 방향을 부드럽게 반영하고, 앉아 있을 때는 달리기 해제
        _animator.SetFloat(MoveXHash,move.x,_blendTime,Time.deltaTime);
        _animator.SetFloat(MoveYHash,move.y,_blendTime,Time.deltaTime);
        _animator.SetBool(IsRunningHash,_playerMovement.IsRunning && !isCrouching );

        _animator.SetBool(IsCrouchingHash,isCrouching); 
        _animator.SetBool(ISInCoverHash,isInCover);
        //앉은 상태의 이동량을 0~1 범위로 전달
        float crouchSpeed = Mathf.Clamp01(move.magnitude);
        _animator.SetFloat(CrouchSpeedHash, crouchSpeed, _blendTime, Time.deltaTime);
        if(_playerCover != null)
        {
            _animator.SetFloat(CoverSideHash, _playerCover.CoverSide);
            float coverSpeed = isInCover ? Mathf.Abs(_playerMovement.CoverMove) : 0f;

            _animator.SetFloat(CoverSpeedHash,coverSpeed,_blendTime, Time.deltaTime);
        }
    }

    
}
