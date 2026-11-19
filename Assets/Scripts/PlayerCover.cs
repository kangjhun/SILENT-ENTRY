using UnityEngine;
//벽에 기대기, 엄폐 이동, 엄폐 해제 처리
[RequireComponent (typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerCover : MonoBehaviour
{
    [SerializeField] Transform _visualRoot;
    [SerializeField, Min(0f)] float _visualWallOffset = 0.3f;

    [SerializeField, Min(0.1f)] float _coverSpeed = 1.5f;
    [SerializeField, Min(0.1f)] float _enterDistance = 1.5f;
    [SerializeField, Min(0f)] float _wallGap = 0f;
    [SerializeField, Min(0.1f)] float _attachSpeed = 3f;

    [Header("엄폐 끝 장애물 검사")]
    [Tooltip("벽과 문 등 관찰을 막는 장애물 레이어")]
    [SerializeField] LayerMask _coverObstacleMask;

    [Tooltip("옆으로 이어진 벽을 확인할 거리")]
    [SerializeField, Min(0.1f)] float _cornerCheckDistance = 1f;

    [SerializeField, Min(0.01f)] float _cornerProbeRadius = 0.08f;

    [Header("엄폐 시야 피격점 보정")]
    [SerializeField] Transform _visionTarget;
    [SerializeField, Min(0f)] float _coverVisionInset = 0.22f;//벽 쪽으로 몸을 바짝 붙이는 깊이
    Vector3 _visionOriginalLocalPos;

    CharacterController _controller;
    PlayerMovement _movement;

    Collider _wall;
    Vector3 _wallNormal;//벽 표면에서 바깥으로 향하는 방향
    Vector3 _wallPoint;
    Vector3 _visualOriginalLocalPosition;

    public bool IsInCover {  get; private set; }
    public float CoverSpeed => _coverSpeed;
    //벽 표면에서 바깥으로 향하는 방향
    public Vector3 WallNormal => _wallNormal;
    //벽을 따라 플레이어의 오른쪽으로 향하는 향
    public Vector3 WallRight => Vector3.Cross(Vector3.up, _wallNormal).normalized;
    //플레이어 중심을 벽 검사 기준점으로 사용
    public Vector3 ProbePosition => transform.TransformPoint(_controller.center);
    //서 있고 바닥에 닿아 있을 때만 엄폐 진입 가능
    public bool CanEnter => !IsInCover && !_movement.IsCrouching && _controller.isGrounded;
    //-1: 왼쪽, 1: 오른쪽 
    //멈ㅊ춰도 마지막 엄폐 방향 유지
    public int CoverSide { get; private set; } = 1;

    public void SetCoverSide(int side)
    {
        if (side == 0) return;
        CoverSide = side > 0 ? 1 : -1;
    }
    public void SwitchCoverSide()
    {
        if(!IsInCover) return;
        CoverSide *= -1;
    }
    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>();
        // 엄폐 해제 시 복구할 모델 위치 저장
        if (_visualRoot != null)
        {
            _visualOriginalLocalPosition = _visualRoot.localPosition;
        }
        // 엄폐 해제 시 복구할 시야 타겟 위치 저장
        if (_visionTarget != null)
        {
            _visionOriginalLocalPos = _visionTarget.localPosition;
        }
    }

    public void EnterCover(Collider wall)
    {
        if(!CanEnter || wall == null) return;
        //문은 엄폐 대상으로 사용하지 않음
        if (wall.GetComponentInParent<DoorController>() != null) return;
        Vector3 origin = ProbePosition;
        Vector3 direction = wall.ClosestPoint(origin) - origin;

        if (direction.sqrMagnitude < 0.0001f) return;

        //엄폐할 벽 표면의 위치와 방향 확인
        if(!wall.Raycast(new Ray(origin,direction.normalized), out RaycastHit hit, _enterDistance))
        {
            return;
        }

        //바닥이나 경사진 면은 제외
        if (Mathf.Abs(hit.normal.y) > 0.1f) return;

        _wall = wall;
        _wallPoint = hit.point;
        _wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;

        //캐릭터의 등이 벽을 향하도록 회전
        transform.rotation = Quaternion.LookRotation(_wallNormal);
        IsInCover = true;
        // 모델을 벽 쪽으로 옮겨 기대는 모습 조정
        if (_visualRoot != null)
        {
            _visualRoot.localPosition = _visualOriginalLocalPosition;
            _visualRoot.position -= _wallNormal * _visualWallOffset;
        }
        // 시야 피격점도 벽 안쪽으로 바짝 당겨 모서리 노출 방지
        if (_visionTarget != null)
        {
            _visionTarget.localPosition = _visionOriginalLocalPos - Vector3.forward * _coverVisionInset;
        }


    }

    public void ExitCover()
    {
        IsInCover = false;
        _wall = null;
        // 모델을 원래 위치로 복구
        if (_visualRoot != null)
        {
            _visualRoot.localPosition = _visualOriginalLocalPosition;
        }
        // 시야 피격점을 원래 위치로 복구
        if (_visionTarget != null)
        {
            _visionTarget.localPosition = _visionOriginalLocalPos;
        }
    }

    //엄폐 중 사용할 이동 속도 계산
    public Vector3 GetVelocity(float horizontal)
    {
        if(!IsInCover) return Vector3.zero;
        if(Mathf.Abs(horizontal) > 0.1f)
        {
            SetCoverSide(horizontal > 0f ? 1 : -1);
        }
        if(_wall == null || !_wall.enabled || !_wall.gameObject.activeInHierarchy)
        {
            ExitCover();
            return Vector3.zero;
        }
        
        //등 뒤에 엄폐할 벽이 없으면 해제
        if (!HasWall(ProbePosition))
        {
            ExitCover();
            return Vector3.zero;
        }

        transform.rotation = Quaternion.LookRotation(_wallNormal);

        //벽을 따라 좌우로 이동
        Vector3 wallRight = Vector3.Cross(Vector3.up, _wallNormal);
        Vector3 sideVelocity = wallRight * horizontal * _coverSpeed;

        if(Mathf.Abs(horizontal) > 0.001f)
        {
            //이동할 쪽을 미리 검사해 벽 끝을 벗어나지 않도록 제한
            Vector3 edgeProbe = ProbePosition + sideVelocity * Time.deltaTime + wallRight * Mathf.Sign(horizontal) * _controller.radius;

            if (!HasWall(edgeProbe))
            {
                sideVelocity = Vector3.zero;
            }
        }

        //벽과 일정한 간격을 유지하도록 이동 속도 조정
        float distance = Vector3.Dot(ProbePosition - _wallPoint, _wallNormal);
        float desiredDistance = _controller.radius +  _wallGap;

        float correctionSpeed = Mathf.Clamp((desiredDistance - distance) / Mathf.Max(Time.deltaTime, 0.0001f), -_attachSpeed, _attachSpeed);

        return sideVelocity + _wallNormal * correctionSpeed;
    }

    bool HasWall(Vector3 origin)
    {
        //벽 쪽으로 검사해 표면이 이어지는지 확인
        if(!_wall.Raycast(new Ray(origin,-_wallNormal), out RaycastHit hit,_enterDistance + _controller.radius))
        {
            return false;
        }

        //처음 엄폐한 면과 방향이 거의 같아야 유지
        return Vector3.Dot(hit.normal, _wallNormal) > 0.95f;
    }
    //side: -1은 벽을 따른 왼쪽, 1은 오른쪽
    public bool IsAtCoverEdge(int side, float extraDistance)
    {
        if(!IsInCover || _wall == null || !_wall.enabled || !_wall.gameObject.activeInHierarchy) return false;

        Vector3 sideDirection = WallRight * (side >= 0 ? 1f : -1f);
        float edgeDistance = _controller.radius + Mathf.Max(0f, extraDistance);

        Vector3 edgeProbe = ProbePosition + sideDirection * edgeDistance;

        //현재 벽이 이어지고 있으면 끝 x
        if (HasWall(edgeProbe))
            return false;
        //다른 콜라이더의 벽이 뒤쪽에 이어져 있어도 열린 통로로 판단 x
        if (Physics.Raycast(edgeProbe, -_wallNormal, _enterDistance + _controller.radius, _coverObstacleMask, QueryTriggerInteraction.Ignore))
            return false;
        //관찰하려는 공간에 벽이나 장애물이 있는지 확인
        float checkDistance = Mathf.Max(edgeDistance,_cornerCheckDistance);
        //검사 시작점이 장애물 안이면 관찰 x
        if (Physics.CheckSphere(ProbePosition, _cornerProbeRadius, _coverObstacleMask, QueryTriggerInteraction.Ignore))
            return false;
        if(Physics.SphereCast(ProbePosition,_cornerProbeRadius,sideDirection,out RaycastHit hit, checkDistance,_coverObstacleMask,QueryTriggerInteraction.Ignore))
            return false;
        //현재 벽이 끝났고 옆 공간도 막혀 있지 않음
        return true;
    }
    

    void OnDisable()
    {
        ExitCover();
    }
}
