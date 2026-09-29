using UnityEngine;

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

    CharacterController _controller;
    PlayerMovement _movement;

    Collider _wall;
    Vector3 _wallNormal;
    Vector3 _wallPoint;
    Vector3 _visualOriginalLocalPosition;

    public bool IsInCover {  get; private set; }
    public float CoverSpeed => _coverSpeed;
    //플레이어 중심 높이에서 벽 검사
    public Vector3 ProbePosition => transform.TransformPoint(_controller.center);
    public bool CanEnter => !IsInCover && !_movement.IsCrouching && _controller.isGrounded;
     void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>(); 
        if(_visualRoot != null)
        {
            _visualOriginalLocalPosition = _visualRoot.localPosition;
        }
    }

    public void EnterCover(Collider wall)
    {
        if(!CanEnter || wall == null) return;

        Vector3 origin = ProbePosition;
        Vector3 direction = wall.ClosestPoint(origin) - origin;

        if (direction.sqrMagnitude < 0.0001f) return;

        //해당 벽 표면의 위치와 방향 확인 
        if(!wall.Raycast(new Ray(origin,direction.normalized), out RaycastHit hit, _enterDistance))
        {
            return;
        }

        
        if (Mathf.Abs(hit.normal.y) > 0.1f) return;

        _wall = wall;
        _wallPoint = hit.point;
        _wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;

        
        //벽 바깥을 바라보면 캐릭터의 등이 벽을 향함
        transform.rotation = Quaternion.LookRotation(_wallNormal);
        IsInCover = true;

        if(_visualRoot != null)
        {
            float offset = _visualWallOffset > 0f ? _visualWallOffset : _controller.radius;
            _visualRoot.localPosition = _visualOriginalLocalPosition + Vector3.back * offset;
        }

        
    }

    public void ExitCover()
    {
        IsInCover = false;
        _wall = null;

        if(_visualRoot != null )
        {
            _visualRoot.localPosition = _visualOriginalLocalPosition;
        }
    }

    
    public Vector3 GetVelocity(float horizontal)
    {
        if(!IsInCover) return Vector3.zero;
        if(_wall == null || !_wall.enabled || !_wall.gameObject.activeInHierarchy)
        {
            ExitCover();
            return Vector3.zero;
        }
        
        //햔제 위치 뒤에 같은 벽이 있는지 확인
        if (!HasWall(ProbePosition))
        {
            ExitCover();
            return Vector3.zero;
        }

        transform.rotation = Quaternion.LookRotation(_wallNormal);

        //벽을 따라가는 캐릭터 기준 오른쪽 방향
        Vector3 wallRight = Vector3.Cross(Vector3.up, _wallNormal);
        Vector3 sideVelocity = wallRight * horizontal * _coverSpeed;

        if(Mathf.Abs(horizontal) > 0.001f)
        {
            //이동할 위치보다 반지름만큼 앞을 더 검사
            Vector3 edgeProbe = ProbePosition + sideVelocity * Time.deltaTime + wallRight * Mathf.Sign(horizontal) * _controller.radius;

            if (!HasWall(edgeProbe))
            {
                sideVelocity = Vector3.zero;
            }
        }

        //캡슐이 벽과 일정한 간격을 유지하도록 보정
        float distance = Vector3.Dot(ProbePosition - _wallPoint, _wallNormal);

        float desiredDistance = _controller.radius +  _wallGap;

        float correctionSpeed = Mathf.Clamp((desiredDistance - distance) / Mathf.Max(Time.deltaTime, 0.0001f), -_attachSpeed, _attachSpeed);

        return sideVelocity + _wallNormal * correctionSpeed;
    }

    bool HasWall(Vector3 origin)
    {
        if(!_wall.Raycast(new Ray(origin,-_wallNormal), out RaycastHit hit,_enterDistance + _controller.radius))
        {
            return false;
        }

        //처음 은폐했던 면과 같은 방향인지 확인
        return Vector3.Dot(hit.normal, _wallNormal) > 0.95f;
    }

    void OnDisable()
    {
        ExitCover();
    }
}
