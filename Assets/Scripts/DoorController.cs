using UnityEngine;

public class DoorController : MonoBehaviour,IInteractable
{
    [SerializeField] Transform _hinge;
    [SerializeField] Collider _leafCollider;
    [SerializeField] Transform _interactionPoint;

    [SerializeField] BoxCollider _passage;
    [SerializeField] BoxCollider _clearanceZone;
    [SerializeField] LayerMask _characterMask;

    [SerializeField] float _openAngle = 90f;
    [SerializeField, Min(1f)] float _rotationSpeed = 180f;

    [SerializeField] bool _autoClose = true;
    [SerializeField, Min(0.1f)] float _autoCloseDelay = 3f;

    Quaternion _closedRotation;

    float _angle;
    float _lastOpenRequestTime;

    bool _targetOpen;
    bool _ready;

    public string DisplayName => _targetOpen ? "문 닫기" : "문 열기";

    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : (_leafCollider != null ? _leafCollider.bounds.center : transform.position);

    public bool IsMoving => Mathf.Abs(_angle - (_targetOpen ? _openAngle : 0f)) > 0.01f;

    public bool CanInteract => _ready && isActiveAndEnabled && !IsMoving;
    public bool IsPassable => _ready && isActiveAndEnabled && _targetOpen && !IsMoving;

    void Awake()
    {
        if(_hinge == null || _leafCollider == null || _passage == null || _clearanceZone == null || _characterMask.value == 0)
            enabled = false; return;

        _closedRotation = _hinge.localRotation;

        _passage.isTrigger = true;
        _clearanceZone.isTrigger = true;

        _ready = true;
    }

    void Update()
    {
        if(!_ready) return;

        if (!_targetOpen && _angle != 0f && IsOccupied())
            RequestOpen();

        float targetAngle = _targetOpen ? _openAngle : 0f;

        _angle = Mathf.MoveTowards(_angle, targetAngle, _rotationSpeed * Time.deltaTime);

        _hinge.localRotation = _closedRotation * Quaternion.AngleAxis(_angle, Vector3.up);

        if (_autoClose && IsPassable && Time.deltaTime - _lastOpenRequestTime >= _autoCloseDelay)
            TryClose();
    }

    public void Interact()
    {
        if(!CanInteract) return;
        if (_targetOpen)
        {
            if (!TryClose())
            {
                Debug.Log("문 주변에 캐릭터가 있어 닫을 수 없습니다.", this);
            }
        }
        else
        {
            RequestOpen();
        }
    }

    public void RequestOpen()
    {
        if(!_ready || !isActiveAndEnabled) return;
        _lastOpenRequestTime = Time.time;
        _targetOpen = true;
    }

    bool TryClose()
    {
        if (IsOccupied())
        {
            return false;
        }

        _targetOpen = false;
        return true;
    }

    bool IsOccupied()
    {
        Transform zone = _clearanceZone.transform;

        Vector3 scale = zone.lossyScale;
        Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        Vector3 halfSize = Vector3.Scale(_clearanceZone.size * 0.5f, absoluteScale);

        Collider[] hits = Physics.OverlapBox(zone.TransformPoint(_clearanceZone.center), halfSize, zone.rotation, _characterMask, QueryTriggerInteraction.Ignore);

        foreach(Collider hit in hits)
        {
            if (hit != null && !hit.transform.IsChildOf(transform)) return true;

        }

        return false;
    }

    public bool IntersectsSegment(Vector3 start, Vector3 end, float agentRadius, out float fraction)
    {
        fraction = 0f;
        if(!gameObject.activeInHierarchy || _passage == null) return false;

        Transform space = _passage.transform;

        Vector3 localStart = space.InverseTransformPoint(start);
        Vector3 localEnd = space.InverseTransformPoint(end);

        Vector3 scale = space.lossyScale;

        Vector3 padding = new Vector3(agentRadius / Mathf.Max(Mathf.Abs(scale.x), 0.0001f), 0f, agentRadius / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));

        Bounds box = new Bounds(_passage.center, _passage.size + padding * 2f);

        if (box.Contains(localStart)) return true;

        Vector3 direction = localEnd - localStart;

        float length = direction.magnitude;

        if(length < 0.0001f) return false;

        Ray ray = new Ray(localStart, direction / length);

        if(!box.IntersectRay(ray,out float distance) || distance > length) return false;

        fraction = Mathf.Clamp01(distance / length);
        return true;
    }

}
