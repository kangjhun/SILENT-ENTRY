using UnityEngine;

[RequireComponent (typeof(CharacterController),typeof(PlayerMovement))]
public class PlayerNoise : MonoBehaviour
{
    [SerializeField] EnemyFSM[] _enemies;

    //웅크리기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _crouchNoiseRadius = 0.8f;
    [SerializeField, Min(0.05f)] float _crouchInterval = 0.8f;

    //걷기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _walkNoiseRadius = 2f;
    [SerializeField, Min(0.05f)] float _walkInterval = 0.3f;

    //달리기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _runNoiseRadius = 6f;
    [SerializeField, Min(0.05f)] float _runInterval = 0.2f;

    [SerializeField, Min(0.01f)] float _minimumMoveSpeed = 0.1f;

    [SerializeField] bool _showDebug;

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
        //다시 활성화될 때 이전 위치와 소음 타이머 초기화
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

        //수직 이동을 제외한 실제 수평 속도 계산
        movement.y = 0f;
        float speed = movement.magnitude / Time.deltaTime;

        //공중에 있거나 거의 움직이지 않으면 소음 발생 중단
        if(!_controller.isGrounded || speed < _minimumMoveSpeed)
        {
            _stepTimer = 0f; return;
        }

        bool isCrouching = _movement != null && _movement.IsCrouching;
        //실제 속도를 기준으로 걷기, 달리기 소음 설정
        bool isRunning = !isCrouching && _movement != null && _movement.IsRunning;

        float interval;
        float radius;
        string movementType;

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
            _stepTimer += Time.deltaTime;

        if (_stepTimer < interval) return;

        _stepTimer = 0f;
        EmitNoise(radius, movementType);
    }

    void EmitNoise(float radius, string movementType)
    {
        if (radius <= 0f) return;

        Vector3 noisePosition = transform.position;

        //등록된 적에게 소음 위치와 반경 전달
        if(_enemies != null)
        {
            foreach(EnemyFSM enemy in _enemies)
            {
                if(enemy == null) continue;

                enemy.HearNoise(noisePosition,radius);
            }
        }

        if (_showDebug)
        {

            Debug.Log($"{movementType} 소음 발생 / 반경 : {radius}", this);
        }
    }
}
