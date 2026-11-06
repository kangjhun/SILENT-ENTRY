using UnityEngine;

//거리, 시야각, 장애물을 확인해 플레이어 감지
public class EnemyVision : MonoBehaviour
{
    [Header("탐지 기준점")]
    [SerializeField] Transform _eyePoint;
    [SerializeField] Transform _playerTarget;

    [Header("시야 범위")]
    [SerializeField, Min(0.1f)]
    float _viewRadius = 8f;

    [SerializeField, Range(1f, 360f)]
    float _viewAngle = 90f;

    [Header("시야를 가리는 장애물")]
    [SerializeField] LayerMask _obstacleMask;

    bool _canSeePlayer;

    public bool CanSeePlayer => _canSeePlayer;
    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public LayerMask ObstacleMask => _obstacleMask;
    //눈 위치가 없으면 현재 오브젝트 위치 사용
    public Vector3 EyePosition => _eyePoint != null ? _eyePoint.position : transform.position;
    public Transform PlayerTarget => _playerTarget;

    void Update()
    {
        CheckPlayer(EyePosition, GetFlatForward());
    }

    public Vector3 GetFlatForward()
    {
        //위아래 기울기를 제외한 수평 정면 방향 계산
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        
        //수평 방향을 구할 수 없으면 월드 앞쪽 방향 사용
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        return forward.normalized;
    }

    public bool CheckObstacle(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        //지정된 장애물 레이어만 검사하고 Trigger는 제외
        return Physics.Raycast(origin, direction, out hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
    }
    void CheckPlayer(Vector3 eyePosition, Vector3 forward)
    {
        _canSeePlayer = IsPlayerVisible(eyePosition, forward);
    }

    bool IsPlayerVisible(Vector3 eyePosition, Vector3 forward)
    {
        //대상이 없거나 비활성 상태이면 감지하지 않음
        if (_playerTarget == null || !_playerTarget.gameObject.activeInHierarchy) return false;
        Vector3 toPlayer = _playerTarget.position - eyePosition;
        float distance = toPlayer.magnitude;
        //시야 거리 밖이면 감지하지 않음
        if (distance > _viewRadius) return false;
        //두 기준점이 겹치면 감지하고 방향 계산 생략
        if (distance <= 0.001f) return true;

        //높이 차이를 제외하고 좌우 시야각 확인
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float angle = Vector3.Angle(forward, flatToPlayer);

        if (angle > _viewAngle * 0.5f) return false;

        //눈과 플레이어 사이에 장애물이 없으면 감지
        bool blocked = CheckObstacle(eyePosition, toPlayer / distance, distance, out _);
        return !blocked;
    }

     void OnDisable()
    {
        //비활성화되면 감지 결과 초기화
        _canSeePlayer = false;
    }
}
