using UnityEngine;

[RequireComponent (typeof(CharacterController))]
public class PlayerNoise : MonoBehaviour
{
    [SerializeField] EnemyFSM[] _enemies;

    [SerializeField, Min(0f)] float _walkNoiseRadius = 2f;
    [SerializeField, Min(0.05f)] float _walkInterval = 0.6f;

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
        _previousPosition = transform.position;
        _stepTimer = 0f;
    }

     void LateUpdate()
    {
        Vector3 movement = transform.position - _previousPosition;

        _previousPosition = transform.position;

        if(Time.deltaTime <= 0f || !_controller.enabled)
        {
            _stepTimer = 0f;
            return;
        }

        movement.y = 0f;
        float speed = movement.magnitude / Time.deltaTime;

        if(!_controller.isGrounded || speed < _minimumMoveSpeed)
        {
            _stepTimer = 0f; return;
        }

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
