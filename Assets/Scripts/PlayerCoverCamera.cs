using UnityEngine;
using Unity.Cinemachine;
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
[RequireComponent(typeof(CinemachineInputAxisController))]
public class PlayerCoverCamera : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] PlayerCover _cover;

    [Header("엄폐 카메라 배치")]
    [SerializeField, Min(0.1f)] float _coverRadius = 1.8f;

    [SerializeField] float _coverHeight = 1.5f;

    [Tooltip("카메라 회전 중심을 벽 바깥으로 이동하는 거리")]
    [SerializeField, Min(0f)] float _outwardOffset = 0.5f;

    [Tooltip("카메라 회전 중심을 관찰 방향으로 이동하는 거리")]
    [SerializeField, Min(0f)] float _sideOffset = 0.35f;

    [Tooltip("캐릭터를 화면 좌우로 배치하는 정도")]
    [SerializeField, Range(0f, 0.4f)] float _screenOffset = 0.22f;

    [Header("엄폐 중 카메라 조작")]
    [SerializeField] KeyCode _switchSideKey = KeyCode.Q;

    [SerializeField, Min(0f)]
    float _horizontalSensitivity = 1f;

    [SerializeField, Min(0f)]
    float _verticalSensitivity = 0.4f;

    [SerializeField, Range(0f, 60f)] float _horizontalLimit = 20f;

    [SerializeField] Vector2 _verticalRange = new Vector2(-10f, 20f);

    [Header("카메라 전환")]
    [SerializeField, Min(0.01f)] float _blendSpeed = 10f;

    CinemachineOrbitalFollow _orbit;
    CinemachineRotationComposer _composer;
    CinemachineInputAxisController _inputController;

    float _normalRadius;
    Vector3 _normalOffset;
    Vector2 _normalScreenPosition;

    bool _wasInCover;
    //1이면 벽을 따라 오른쪽, -1이면 왼쪽 관찰
    int _side = 1;

    float _lookYaw;
    float _lookPitch;

    void Awake()
    {
        _orbit = GetComponent<CinemachineOrbitalFollow>();
        _composer = GetComponent<CinemachineRotationComposer>();
        _inputController = GetComponent<CinemachineInputAxisController>();

        if(_cover == null)
        {
            Debug.LogError("PlayerCoverCamera에 PlayerCover를 연겷세요.");
            enabled = false;
            return;
        }
        SaveNormalSettings();
    }
    
    void SaveNormalSettings()
    {
        //엄폐 해제 시 돌아갈 기존 설정 저장
        _normalRadius = _orbit.Radius;
        _normalOffset = _orbit.TargetOffset;
        _normalScreenPosition = _composer.Composition.ScreenPosition;
    }



    void Update()
    {
        bool inCover = _cover.IsInCover;
        bool canLook = Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f;
        //평소에는 Cinemachine이 마우스 입력 처리
        //엄폐 중에는 이 스크립트가 직접 처리
        _inputController.enabled = canLook && !inCover;

        if(inCover && !_wasInCover)
        {
            SaveNormalSettings();

            //현재 카메라 방향과 가까운 관찰 방향 선택
            Camera mainCamera = Camera.main;

            Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : _cover.WallRight;

            _side = Vector3.Dot(cameraForward,_cover.WallRight) >= 0f ? 1 : -1;

            _lookYaw = 0f;
            _lookPitch = Mathf.Clamp(_orbit.VerticalAxis.Value, _verticalRange.x, _verticalRange.y);
        }
        if(inCover && canLook)
        {
            //Q키로 좌우 관찰 방향 전환
            if (Input.GetKeyDown(_switchSideKey))
            {
                _side *= -1;
                _lookYaw = 0f;
            }
            _lookYaw += Input.GetAxis("Mouse X") * _verticalSensitivity;
            _lookPitch -= Input.GetAxis("Mouse Y") * _verticalSensitivity;

            //벽 뒤로 카메라가 돌아가지 않도록 범위 제한
            _lookYaw = Mathf.Clamp(_lookYaw, -_horizontalLimit, _horizontalLimit);

            _lookPitch = Mathf.Clamp(_lookPitch, _verticalRange.x, _verticalRange.y);
        }

        float blend = 1f - Mathf.Exp(-_blendSpeed * Time.deltaTime);

        Vector3 targetOffset = _normalOffset;
        Vector2 targetScreen = _normalScreenPosition;
        float targetRadius = _normalRadius;

        if (inCover)
        {
            Vector3 alongWall = _cover.WallRight * _side;
            //벽을 따라 보면서 약간 벽 바깥쪽을 향하도록 설정
            Vector3 viewDirection = -_cover.WallNormal;
            float baseYaw = Mathf.Atan2(viewDirection.x, viewDirection.z) * Mathf.Rad2Deg;
            _orbit.HorizontalAxis.Value = Mathf.LerpAngle(_orbit.HorizontalAxis.Value, baseYaw + _lookYaw, blend);
            _orbit.VerticalAxis.Value = Mathf.Lerp(_orbit.VerticalAxis.Value, _lookPitch, blend);

            //카메라 회전 중심을 벽 바깥과 관찰 방향으로 이동
            Vector3 worldOffset = Vector3.up * _coverHeight + _cover.WallNormal * _outwardOffset + alongWall * _sideOffset;
            //월드 방향을 Player의 로컬 방향으로 변환
            targetOffset = _cover.transform.InverseTransformDirection(worldOffset);

            targetScreen = new Vector2(-_side * _screenOffset, _normalScreenPosition.y);

            targetRadius = _coverRadius;

        }
        //엄폐 진입과 해제 시 배치를 부드럽게 변경
        _orbit.TargetOffset = Vector3.Lerp(_orbit.TargetOffset, targetOffset, blend);
        _orbit.Radius = Mathf.Lerp(_orbit.Radius, targetRadius, blend);

        var composition = _composer.Composition;

        composition.ScreenPosition = Vector2.Lerp(composition.ScreenPosition, targetScreen, blend);
        _composer.Composition = composition;
        _wasInCover = inCover;
    }
}
