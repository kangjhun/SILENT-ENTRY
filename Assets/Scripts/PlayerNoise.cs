using UnityEngine;
//플레이어의 이동 상태에 따라 적에게 발소리 전달
[RequireComponent (typeof(CharacterController),typeof(PlayerMovement))]
public class PlayerNoise : MonoBehaviour
{
    [SerializeField] EnemyFSM[] _enemies;

    //웅크리기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _crouchNoiseRadius = 0.8f;
    [SerializeField, Min(0.05f)] float _crouchInterval = 0.8f;

    //걷기 소음 범위와 발생 간격
    [SerializeField, Min(0f)] float _walkNoiseRadius = 2f;
    [SerializeField, Min(0.05f)] float _walkInterval = 0.3f;

    //달리기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _runNoiseRadius = 6f;
    [SerializeField, Min(0.05f)] float _runInterval = 0.2f;

    [SerializeField, Min(0.01f)] float _minimumMoveSpeed = 0.1f;

    CharacterController _controller;
    PlayerMovement _movement;

    Vector3 _previousPosition;
    float _stepTimer;

     void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>();
    }

     void OnEnable()
    {
        //다시 활성화되면 위치와 소음 타이머 초기화
        _previousPosition = transform.position;
        _stepTimer = 0f;
    }

     void LateUpdate()
    {
        //이동 처리가 끝난 뒤 실제 이동량 확인
        Vector3 movement = transform.position - _previousPosition;

        _previousPosition = transform.position;

        if(Time.deltaTime <= 0f || !_controller.enabled)
        {
            _stepTimer = 0f;
            return;
        }

        //높이 변화를 제외한 실제 이동 속도 계산
        movement.y = 0f;
        float speed = movement.magnitude / Time.deltaTime;

        //공중에 있거나 거의 움직이지 않으면 소음 중단
        if(!_controller.isGrounded || speed < _minimumMoveSpeed)
        {
            _stepTimer = 0f; return;
        }

        bool isCrouching = _movement != null && _movement.IsCrouching;
        bool isRunning = !isCrouching && _movement != null && _movement.IsRunning;

        float interval;
        float radius;
        string movementType;

        //이동 상태에 맞는 소음 범위와 간격 선택
        if (isCrouching)
        {
            interval = _crouchInterval;
            radius = _crouchNoiseRadius;
            movementType = "웅크리기";
        }else if (isRunning)
        {
            interval = _runInterval;
            radius = _runNoiseRadius;
            movementType = "달리기";
        }
        else
        {
            interval = _walkInterval;
            radius = _walkNoiseRadius;
            movementType = "걷기";
        }
        //설정된 간격마다 소음 발생
        _stepTimer += Time.deltaTime;

        if (_stepTimer < interval) return;

        _stepTimer = 0f;
        EmitNoise(radius, movementType);
    }

    void EmitNoise(float radius, string movementType)
    {
        if (radius <= 0f) return;

        Vector3 noisePosition = transform.position;

        //연결된 적에게 소리가 난 위치와 들리는 범위 전달
        if(_enemies != null)
        {
            foreach(EnemyFSM enemy in _enemies)
            {
                if(enemy == null) continue;

                enemy.HearNoise(noisePosition,radius);
            }
        }

    }
}
