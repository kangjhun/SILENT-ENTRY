using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] Transform _player;
    [SerializeField] Transform _cameraTransform;

    [Header("카메라 배치")]
    [SerializeField] float _pivotHeight = 1.5f;
    [SerializeField,Min(0.1f)] float _distance = 3f;
    [SerializeField] float _shoulderOffset = -0.5f;

    [Header("마우스 회전")]
    [SerializeField,Min(0f)] float _mouseSensitivity = 1f;
    [SerializeField] float _minPitch = -10f;
    [SerializeField] float _maxPitch = 35f;

    [SerializeField, Min(0f)]
    float _verticlaSensitivity = 0.4f;

    float _yaw; //좌우 회전 각도
    float _pitch = 20f; //위아래 회전 각도
    void Start()
    {
        if(_player == null || _cameraTransform == null)
        {
            Debug.LogError("player와 main camera를 연결하세요.", this);
            enabled = false;
            return;
        }

        //시작할 때 플레이어가 바라보는 방향에 카메라를 맞춤
        _yaw = _player.eulerAngles.y;
        //초기 각도도 설정된 상하 회전 범위 안으로 제한
        _pitch = Mathf.Clamp( _pitch,_minPitch, _maxPitch); 
        SetCursorLocked(true);
        UpdateCamera();
    }

    void Update()
    {
        //ESC를 누른 순간 커서를 해제
        if (Input.GetKey(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }else if(Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            //왼쪽 클릭으로 카메라 조작 재게
            SetCursorLocked(true);
        }

        //커서가 풀렸거나 게임 시간이 정지됐으면 회전 입력 무시
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale == 0f) return;

        //마우스 이동량으로 회전 각도 누적
        _yaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
        _pitch += Input.GetAxis("Mouse Y") * _verticlaSensitivity;

        //카메라가 지나치게 위아래로 회전하지 않도록 제한
        _pitch = Mathf.Clamp(_pitch,_minPitch, _maxPitch);  
    }

     void LateUpdate()
    {
        //플레이어의 Update 이동 처리가 끝난 뒤 카메라 배치
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (_player == null || _cameraTransform == null) return;
        //CameraRig를 플레이어의 지정 높이로 이동
        transform.position = _player.position + Vector3.up * _pivotHeight;
        //CameraRig를 회전시켜 자식 카메라가 주변을 돌도록 처리
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        //CameraRig 기준 옆으로 이동하고 뒤로 떨어져 배치
        _cameraTransform.localPosition = new Vector3(_shoulderOffset, 0f, -_distance);

        //카메라가 CameraRig와 같은 방향을 바라보게 설정
        _cameraTransform.localRotation = Quaternion.identity;
    }

    void SetCursorLocked(bool locked)
    {
        //잠그면 커서를 화면 중앙에 고정하고 숨김
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        //게임 종료 등으로 카메라 조작이 중단되면 커서를 해제
        SetCursorLocked(false);
    }
}//이 코드를 없애고, 지금 시네머신에 followCamera를 사용하니까 여기서 카메라를 followCamera로 옮기고,
// 이동은 playerMove에서 하게 바꾸기

