using UnityEngine;

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
    public Vector3 EyePosition => _eyePoint != null ? _eyePoint.position : transform.position;
    public Transform PlayerTarget => _playerTarget;
    // Update is called once per frame
    void Update()
    {
        CheckPlayer(EyePosition, GetFlatForward());
    }

    public Vector3 GetFlatForward()
    {
        //위아래 기울기를 제거해 수평 방향만 사용
        //방향 기준은 EnemyVision이 붙은 오브젝트
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        
        //정면이 수직에 가까워 수평 방향을 구할 수 없으면 대체
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        return forward.normalized;
    }

    public bool CheckObstacle(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit)
    {
        //지정된 장애물 레이어만 검사하며 Trigger는 무시
        return Physics.Raycast(origin, direction, out hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
    }
    void CheckPlayer(Vector3 eyePosition, Vector3 forward)
    {
        bool detected = IsPlayerVisible(eyePosition, forward);

        //발견, 놓침 상태가 바뀔 때만 로그 출력
        if (detected != _canSeePlayer)
        {
            Debug.Log(detected ? "플레이어 시야에 들어옴" : "플레이어 시야에서 벗어남", this);

        }
        _canSeePlayer = detected;
    }

    bool IsPlayerVisible(Vector3 eyePosition, Vector3 forward)
    {
        //대상이 없거나 부모를 포함해 비활성화된 경우 탐지하지 않음
        if (_playerTarget == null || !_playerTarget.gameObject.activeInHierarchy) return false;
        Vector3 toPlayer = _playerTarget.position - eyePosition;
       //거리 판정에는 높이 차이도 포함
        float distance = toPlayer.magnitude;

        if (distance > _viewRadius) return false;
        //두 기준점이 겹치면 발견으로 처리
        //이후 방향 계산에서 0으로 나누는 것도 방지
        if (distance <= 0.001f) return true;

        //시야각은 수평면에서 판정
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float angle = Vector3.Angle(forward, flatToPlayer);

        //전체 시야각의 절반을 정면 기준 좌우에 적용
        if (angle > _viewAngle * 0.5f) return false;

        //실제 눈 위치에서 플레이어 기준점까지 장애물 검사
        bool blocked = CheckObstacle(eyePosition, toPlayer / distance, distance, out _);
        return !blocked;
    }

     void OnDisable()
    {
        //시야 기능을 껐을 때 이전 발견 결과가 남지 않도록 초기화
        _canSeePlayer = false;
    }
}
