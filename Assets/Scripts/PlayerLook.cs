using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] Transform _player;
    [SerializeField] Transform _cameraTransform;

    [SerializeField] float _pivotHeight = 1.5f;
    [SerializeField] float _distance = 3f;
    [SerializeField] float _shoulderOffset = -0.5f;

    [SerializeField] float _mouseSensitivity = 1f;
    [SerializeField] float _minPitch = -30f;
    [SerializeField] float _maxPitch = 60f;

    float _yaw;
    float _pitch = 20f;
    void Start()
    {
        if(_player == null || _cameraTransform == null)
        {
            Debug.LogError("player와 main camera를 연결하세요.", this);
            enabled = false;
            return;
        }

        _yaw = _player.eulerAngles.y;
        SetCursorLocked(true);
        UpdateCamera();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }else if(Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        _yaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
        _pitch += Input.GetAxis("Mouse Y") * _mouseSensitivity;

        _pitch = Mathf.Clamp(_pitch,_minPitch, _maxPitch);  
    }

     void LateUpdate()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (_player == null || _cameraTransform == null) return;
        transform.position = _player.position + Vector3.up * _pivotHeight;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _cameraTransform.localPosition = new Vector3(_shoulderOffset, 0f, -_distance);

        _cameraTransform.localRotation = Quaternion.identity;
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        SetCursorLocked(false);
    }
}
