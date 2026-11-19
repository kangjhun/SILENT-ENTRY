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

    [Header("벽 끝 자동 관찰")]
    [Tooltip("이 거리만큼 옆을 검사해서 벽 끝 관찰 시작")]
    [SerializeField, Min(0f)] float _edgeEnterDistance = 0.15f;

    [Tooltip("벽 끝 관찰을 유지할 검사 거리. 시작 거리보다 크게 설정")]
    [SerializeField, Min(0f)] float _edgeKeepDistance = 0.3f;

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
    //현재 관찰 중인 벽 끝 방향. 0이면 벽 끝 관찰 중이 아님
    int _edgeSide;

    float _lookYaw;
    float _lookPitch;

    void Awake()
    {
        _orbit = GetComponent<CinemachineOrbitalFollow>();
        _composer = GetComponent<CinemachineRotationComposer>();
        _inputController = GetComponent<CinemachineInputAxisController>();

        if(_cover == null)
        {
            Debug.LogError("PlayerCoverCamera에 PlayerCover를 연결하세요.");
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
            _edgeSide = 0;
            //현재 카메라 방향과 가까운 관찰 방향 선택
            Camera mainCamera = Camera.main;

            Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : _cover.WallRight;

            _side = Vector3.Dot(cameraForward,_cover.WallRight) >= 0f ? 1 : -1;

            _lookYaw = 0f;
            _lookPitch = Mathf.Clamp(_orbit.VerticalAxis.Value, _verticalRange.x, _verticalRange.y);
        }
        //벽 끝에서는 통로가 있는 쪽으로 자동 선택
        if (inCover && canLook)
            UpdateEdgeSide();
        else if (!inCover)
            _edgeSide = 0;
        if (inCover && canLook)
        {
            //좌우 입력에는 수평 감도 사용
            _lookYaw += Input.GetAxis("Mouse X") * _horizontalSensitivity;
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

            //열린 벽 끝에서만 옆으로 이동
            float sideOffset = _edgeSide != 0 ? _sideOffset : 0f;
            //카메라 회전 중심을 벽 바깥과 관찰 방향으로 이동
            Vector3 worldOffset = Vector3.up * _coverHeight + _cover.WallNormal * _outwardOffset + alongWall * sideOffset;
            //월드 방향을 Player의 로컬 방향으로 변환
            targetOffset = _cover.transform.InverseTransformDirection(worldOffset);

            //막힌 위치에서는 캐릭터를 중앙에 배치
            float screenX = _edgeSide != 0 ? -_side * _screenOffset : 0f;
            targetScreen = new Vector2(screenX, _normalScreenPosition.y);

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

    void UpdateEdgeSide()
    {
        float enterDistance = _edgeEnterDistance;
        float keepDistance = Mathf.Max(_edgeKeepDistance,enterDistance);
        //경계에서 카메라가 반복 전환되는 현상 방지
        if(_edgeSide !=0 && _cover.IsAtCoverEdge(_edgeSide, keepDistance))
        {
            _side = _edgeSide;
            return;
        }
        _edgeSide = 0;

        bool leftEdge = _cover.IsAtCoverEdge(1,enterDistance);
        bool rightEdge = _cover.IsAtCoverEdge(-1,enterDistance);

        if(leftEdge && !rightEdge)
        {
            _edgeSide = -1;
        }else if(rightEdge && !leftEdge)
        {
            _edgeSide = 1;
        }else if(leftEdge && rightEdge)
        {
            //좁은 엄폐물에서 양쪽 모두 끝이면 현재 방향 유지
            _edgeSide = _side;
        }

        if (_edgeSide == 0) return;
        //관찰 방향이 바뀔 때 마우스 좌우 보정 초기화
        if (_side != _edgeSide)
            _lookYaw = 0f;
        _side = _edgeSide;
    }
}
