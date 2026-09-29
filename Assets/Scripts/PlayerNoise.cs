using UnityEngine;

[RequireComponent (typeof(CharacterController))]
public class PlayerNoise : MonoBehaviour
{
    [SerializeField] EnemyFSM[] _enemies;

    //걷기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _walkNoiseRadius = 2f;
    [SerializeField, Min(0.05f)] float _walkInterval = 0.6f;

    //달리기 소음의 범위와 발생 간격
    [SerializeField, Min(0f)] float _runNoiseRadius = 6f;
    [SerializeField, Min(0.05f)] float _runInterval = 0.35f;

    [SerializeField, Min(0.01f)] float _minimumMoveSpeed = 0.1f;
    [SerializeField, Min(0.1f)] float _runSpeedThreshold = 4.5f;

    [SerializeField] bool _showDebug;

    CharacterController _controller;

    Vector3 _previousPosition;
    float _stepTimer;

     void Awake()
    {
        _controller = GetComponent<CharacterController>();
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

        //실제 속도를 기준으로 걷기, 달리기 소음 설정
        bool isRunning = speed >= _runSpeedThreshold;

        float interval = isRunning ? _runInterval : _walkInterval;
        float radius = isRunning ? _runNoiseRadius : _walkNoiseRadius;

        _stepTimer += Time.deltaTime;

        if (_stepTimer < interval) return;

        _stepTimer = 0f;
        EmitNoise(radius, isRunning);
    }

    void EmitNoise(float radius, bool isRunning)
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
            string movementType = isRunning ? "달리기" : "걷기";

            Debug.Log($"{movementType} 소음 발생 / 반경 : {radius}", this);
        }
    }
}
