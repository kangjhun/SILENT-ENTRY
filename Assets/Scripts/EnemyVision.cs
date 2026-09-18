using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] Transform _eyePoint;
    [SerializeField] Transform _playerTarget;

    [SerializeField, Min(0.1f)]
    float _viewRadius = 8f;

    [SerializeField, Range(1f, 360f)]
    float _viewAngle = 90f;

    [SerializeField] LayerMask _obstacleMask;

    bool _canSeePlayer;

    public bool CanSeePlayer => _canSeePlayer;
    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public LayerMask ObstacleMask => _obstacleMask;
    public Vector3 EyePosition => _eyePoint != null ? _eyePoint.position : transform.position;
    public Transform PlayerTarget => _playerTarget;
    // Update is called once per frame
    void Update()
    {
        CheckPlayer(EyePosition, GetFlatForward());
    }

    public Vector3 GetFlatForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        return forward.normalized;
    }

    public bool CheckObstacle(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
    }
    void CheckPlayer(Vector3 eyePosition, Vector3 forward)
    {
        bool detected = IsPlayerVisible(eyePosition, forward);

        if (detected != _canSeePlayer)
        {
            Debug.Log(detected ? "플레이어 발견!" : "플레이어 놓침!", this);

        }
        _canSeePlayer = detected;
    }

    bool IsPlayerVisible(Vector3 eyePosition, Vector3 forward)
    {
        if (_playerTarget == null || !_playerTarget.gameObject.activeInHierarchy) return false;
        Vector3 toPlayer = _playerTarget.position - eyePosition;
        float distance = toPlayer.magnitude;

        if (distance > _viewRadius) return false;
        if (distance <= 0.001f) return true;
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float angle = Vector3.Angle(forward, flatToPlayer);

        if (angle > _viewAngle * 0.5f) return false;

        return !CheckObstacle(eyePosition, toPlayer / distance, distance, out _);
    }
}
