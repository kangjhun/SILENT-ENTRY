using UnityEngine;

public class DoorController : MonoBehaviour,IInteractable
{
    [SerializeField] Transform _hinge;//문 회전축
    [SerializeField] Collider _leafCollider;//문짝 콜라이더
    [SerializeField] Transform _interactionPoint;

    [SerializeField] BoxCollider _passage;//문 통로 영역
    [SerializeField] BoxCollider _clearanceZone;//문 주변 캐릭터 감지 영역
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
    //지정된 상호작용 위치가 없으면 문짝 중심을 사용
    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : (_leafCollider != null ? _leafCollider.bounds.center : transform.position);

    public bool IsMoving => Mathf.Abs(_angle - (_targetOpen ? _openAngle : 0f)) > 0.01f;

    public bool CanInteract => _ready && isActiveAndEnabled && !IsMoving;
   //문이 완전히 열린 상태에서 통과 가능
    public bool IsPassable => _ready && isActiveAndEnabled && _targetOpen && !IsMoving;

    void Awake()
    {
        //필수 참조와 캐릭터 레이어 설정 확인
        if (_hinge == null || _leafCollider == null || _passage == null || _clearanceZone == null || _characterMask.value == 0)
        {
            Debug.LogError("문의 필수 연결과 Character Mask를 확인하세요.", this);
            enabled = false; 
            return;
        }
        _closedRotation = _hinge.localRotation;

        _passage.isTrigger = true;
        _clearanceZone.isTrigger = true;

        _ready = true;
    }

    void Update()
    {
        if(!_ready) return;
        //닫히는 중 캐릭터가 감지되면 다시 열기
        if (!_targetOpen && _angle != 0f && IsOccupied())
            RequestOpen();

        float targetAngle = _targetOpen ? _openAngle : 0f;
        //목표 각도를 향해 문 회전
        _angle = Mathf.MoveTowards(_angle, targetAngle, _rotationSpeed * Time.deltaTime);

        _hinge.localRotation = _closedRotation * Quaternion.AngleAxis(_angle, Vector3.up);
        //열기 요청 이후 일정 시간이 지나면 자동 닫기 시도
        if (_autoClose && IsPassable && Time.time - _lastOpenRequestTime >= _autoCloseDelay)
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
        //열기 요청 시간을 갱신하고 목표 상태 변경
        _lastOpenRequestTime = Time.time;
        _targetOpen = true;
    }

    bool TryClose()
    {
        //주변에 캐릭터가 있으면 닫기 차단
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
        //월드 스케일을 반영해 감지 영역 크기 계산
        Vector3 scale = zone.lossyScale;
        Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        Vector3 halfSize = Vector3.Scale(_clearanceZone.size * 0.5f, absoluteScale);

        Collider[] hits = Physics.OverlapBox(zone.TransformPoint(_clearanceZone.center), halfSize, zone.rotation, _characterMask, QueryTriggerInteraction.Ignore);

        foreach(Collider hit in hits)
        {//문 자체의 자식 오브젝트는 감지에서 제외
            if (hit != null && !hit.transform.IsChildOf(transform)) return true;

        }

        return false;
    }
    //이동 경로가 문 통로를 지나는지 확인하고 교차 위치 비율 반환
    public bool IntersectsSegment(Vector3 start, Vector3 end, float agentRadius, out float fraction)
    {
        fraction = 0f;
        if(!gameObject.activeInHierarchy || _passage == null) return false;

        Transform space = _passage.transform;
        //경로를 통로의 로컬 좌표로 변환
        Vector3 localStart = space.InverseTransformPoint(start);
        Vector3 localEnd = space.InverseTransformPoint(end);

        Vector3 scale = space.lossyScale;
        //에이전트 반경만큼 통로 검사 범위 확장
        Vector3 padding = new Vector3(agentRadius / Mathf.Max(Mathf.Abs(scale.x), 0.0001f), 0f, agentRadius / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));

        Bounds box = new Bounds(_passage.center, _passage.size + padding * 2f);

        if (box.Contains(localStart)) return true;

        Vector3 direction = localEnd - localStart;

        float length = direction.magnitude;

        if(length < 0.0001f) return false;
        //경로 구간 안에서 통로와 교차하는지 확인
        Ray ray = new Ray(localStart, direction / length);

        if(!box.IntersectRay(ray,out float distance) || distance > length) return false;

        fraction = Mathf.Clamp01(distance / length);
        return true;
    }

}
