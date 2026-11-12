using System;
using UnityEngine;

[RequireComponent (typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
   
    [SerializeField] Transform _camerTransform;

    [SerializeField] float _walkSpeed = 2f;
    [SerializeField] float _runSpeed = 3.2f;
    [SerializeField] float _rotationSpeed = 540f;

    [SerializeField] float _crouchSpeed = 1.2f;
    [SerializeField] float _crouchHeight = 1f;

    //벽 등의 레이러
    [SerializeField] LayerMask _standObstacleMask;

    [SerializeField] float _gravity = -20f;

    [Header("스태미나")]
    [SerializeField, Min(1f)] float _maxStamina = 100f;
    [SerializeField, Min(0.1f)] float _staminaDrainPerSecond = 40f;
    [SerializeField, Min(0.1f)] float _staminaRecoveryPerSecond = 20f;
    [SerializeField, Min(0f)] float _staminaRecoveryDelay = 1f;

    [SerializeField, Range(0.01f, 1f)] float _runResumeRatio = 0.5f;

    float _curretStamina;
    float _staminaRecoveryTimer;

    public bool IsExhausted { get; private set; }
    public float StaminaRatio => Mathf.Clamp01(_curretStamina / MathF.Max(_maxStamina, 1f));
    public bool CanRun => !IsExhausted && _curretStamina > 0f;

    float _standingHeight;
    Vector3 _standingCenter;

    public bool IsCrouching { get; private set; }

    CharacterController _controller;
    float _verticalSpeed;

    PlayerCover _cover;
    PlayerCoinThrower _coinThrower;
    public float MoveSpeed { get; private set; }
    public bool IsRunning {  get; private set; }
    public Vector2 AnimationMove {  get; private set; }
    public float CoverMove { get; private set; }
     void Awake()
    {
        _controller = GetComponent<CharacterController> ();
        _cover = GetComponent<PlayerCover> ();
        _coinThrower = GetComponentInChildren<PlayerCoinThrower> ();
        _standingHeight = _controller.height;
        _standingCenter = _controller.center;
        _curretStamina = _maxStamina;
        _staminaRecoveryTimer = 0f;
        IsExhausted = false;
    }

     void Start()
    {
        SetCursorLocked(true);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

     void OnDisable()
    {
        SetCursorLocked (false);

        IsRunning = false;
        MoveSpeed = 0f;
        AnimationMove = Vector2.zero;
        CoverMove = 0f;
    }
    void Update()
    {

        if (Time.timeScale == 0f) return;

        if(_camerTransform == null) return; 

        bool isInCover = _cover != null && _cover.IsInCover;


        //엄폐 중에는 웅크리기 전환 금지
        if (Cursor.lockState == CursorLockMode.Locked && Input.GetKeyDown(KeyCode.C) && !isInCover)
        {
            if (!IsCrouching)
            {
                SetCrouching(true);
            }else if (CanStand())
            {
                SetCrouching(false);
            }
        }
        //기본 상태는 이동 입력 없음
        Vector2 input = Vector2.zero;
        bool runPressed = false;

        //커서가 잠겨 있을 때만 이동과 입력 받기
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            //왼쪽 Shift 키를 누르고 있는지 확인
            runPressed = Input.GetKey(KeyCode.LeftShift);
        }

        if (isInCover) input.y = 0f;
        //대각선 이동 속도 제한
        input = Vector2.ClampMagnitude(input, 1f);

        //카메라 기준 수평 이동 방향 계산
        Vector3 forward = Vector3.ProjectOnPlane(_camerTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 moveDirection = forward * input.y + right * input.x;

        bool hasInput = input.sqrMagnitude > 0.001f;

        //웅크린 상태거나 엄폐 중이면 달리기 불가
        bool wantsRun = runPressed && hasInput && !IsCrouching && !isInCover && CanRun; 
        float speed = IsCrouching ? _crouchSpeed  : wantsRun ? _runSpeed : _walkSpeed;

        //투척 애니메이션 중에는 투척 방향을 유지
        bool isThrowing = _coinThrower != null && _coinThrower.IsThrowing;
        //이동 방향으로 캐릭터 회전
        //엄폐 중 회전은 PlayerCover가 처리
        if (!isInCover && !isThrowing && Cursor.lockState == CursorLockMode.Locked && hasInput && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        } 

        //지면에서는 작은 하강 속도를 유지하고 중력 적용
        if(_controller.isGrounded && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        _verticalSpeed += _gravity * Time.deltaTime;

        Vector3 velocity;

        if (isInCover)
        {
            //캐릭터의 오른쪽과 카메라의 오른쪽이 같은 방향인지 확인
            float alignment = Vector3.Dot(transform.right, right);
            //서로 반대 방향이면 입력을 뒤집어 화면 기준 좌우를 맞춤
            float coverInput = alignment < 0f ? - input.x : input.x;
            
            velocity = _cover.GetVelocity(coverInput);
        }
        else
        {
            //일반 상태 : 카메라 기준으로 이동
            velocity = moveDirection * speed;
        }
            //두 상태 모두 중력 적용
            velocity.y = _verticalSpeed;

        Vector3 beforePosition = transform.position;

        CollisionFlags flags = _controller.Move(velocity * Time.deltaTime);

        //이번 이동에서 지면에 닿았다면 하강 속도 초기화
        if((flags & CollisionFlags.Below) !=0 && _verticalSpeed < 0f)
        {
            _verticalSpeed = -2f;
        }

        //충돌 처리 후 실제 수평 이동 속도 계산
        Vector3 moved = transform.position - beforePosition;
        moved.y = 0f;

        MoveSpeed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;

        IsRunning = wantsRun && hasInput && MoveSpeed > 0.1f;
        UpdateStamina(IsRunning && _controller.isGrounded);

        //실제 이동 속도를 캐릭터 기준 방향으로 변환하여 애니메이션에 사용
        Vector3 worldVelocity = Time.deltaTime > 0f ? moved / Time.deltaTime : Vector3.zero;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        //실제 좌우 이동 속도를 -1~1로 변환
        //벽에 막혀 움직이지 않으면 0
        CoverMove = _cover != null && _cover.IsInCover ? Mathf.Clamp(localVelocity.x / Mathf.Max(_cover.CoverSpeed, 0.01f), -1f, 1f) : 0f;
        float animationBaseSpeed = IsCrouching ? _crouchSpeed : _walkSpeed;
        AnimationMove = Vector2.ClampMagnitude(new Vector2(localVelocity.x, localVelocity.z) / Mathf.Max(animationBaseSpeed, 0.01f), 1f);

    }
    //동전을 던지는 방향으로 캐릭터 몸 회전
    public void FaceThrowDirection(Vector3 direction)
    {
        //엄페 중 몸 회전은 PlayerCover에서 처리
        if (_cover != null && _cover.IsInCover) return;
        //위아래 기울기를 제외하고 좌우로만 회전
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    void UpdateStamina(bool isActuallyRunning)
    {
        float deltaTime = Time.deltaTime;
        //실제로 달리는 동안 스태미나 소모
        if (isActuallyRunning)
        {
            _curretStamina = MathF.Max(0f, _curretStamina - _staminaDrainPerSecond * deltaTime);
            _staminaRecoveryTimer = _staminaRecoveryDelay;
            if(_curretStamina <= 0f)
                IsExhausted = true;
            return;
        }
        //달리기를 멈춘 뒤 회복 대기 시간 적용
        float recoveryTime = deltaTime;
        if(_staminaRecoveryTimer > 0f)
        {
            float waitTime = Mathf.Min(_staminaRecoveryTimer,recoveryTime);
            _staminaRecoveryTimer -= waitTime;
            recoveryTime -= waitTime;
        }
        //대기 시간이 끝나면 스태미나 회복
        if(recoveryTime > 0f)
        {
            _curretStamina = Mathf.Min(_maxStamina, _curretStamina + _staminaRecoveryPerSecond * recoveryTime);
        }
        //탈진 후 일정량 이상 회복하면 다시 달리기 허용
        if(IsExhausted && StaminaRatio >= _runResumeRatio)
        {
            IsExhausted =false;
        }
    }

    void SetCrouching(bool crouching)
    {
        IsCrouching = crouching;

        //캡슐 지름보다 작아지지 않도록 높이 제한
        float height = crouching ? Mathf.Clamp(_crouchHeight, _controller.radius * 2f, _standingHeight) : _standingHeight;

        Vector3 center = _standingCenter;

        //높이가 줄어든 만큼 중심도 내려 발 위치 유지
        center.y -= (_standingHeight - height) * 0.5f;

        _controller.height = height;
        _controller.center = center;
    }
    //현재 머리 위치부터 일어섰을 때의 머리 위치까지 장애물 확인
    bool CanStand()
    {
        Vector3 up = transform.up;

        Vector3 currentTop = transform.TransformPoint(_controller.center) + up * (_controller.height * 0.5f - _controller.radius);
        Vector3 standingTop = transform.TransformPoint(_standingCenter) + up * (_standingHeight * 0.5f - _controller.radius);

        return !Physics.CheckCapsule(currentTop,standingTop,_controller.radius * 0.95f,_standObstacleMask,QueryTriggerInteraction.Ignore);
    }
}
