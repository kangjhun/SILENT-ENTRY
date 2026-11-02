using System.Collections.Generic;
using UnityEngine;
//주변 상호작용 대상을 선택하고 E키 입력 처리
public class PlayerInteraction : MonoBehaviour
{
    [Header("플레이어 카메라")]
    [SerializeField] Camera _playerCamera;

    [Header("플레이어 가슴 높이의 기준점")]
    [SerializeField] Transform _interactionOrigin;

    [Header("상호작용 설정")]
    [SerializeField, Min(0.1f)] float _interactionDistance = 2f;

    [Header("캐릭터 정면 판정")]
    [SerializeField, Range(1f, 180f)]
    float _interactionAngle = 180f;

    [Header("상호작용 대상과 장애물 레이어")]
    [SerializeField] LayerMask _rayMask;

    [Header("엄폐")]
    [SerializeField] PlayerCover _playerCover;

    [SerializeField, Min(0.01f)] float _targetCheckInterval = 0.05f;
    float _nextTargetCheckTime;

    IInteractable _currentTarget;
    string _cachedDisplayName;
    string _cachedPrompt;

    readonly Collider[] _overlapBuffer = new Collider[32];
    //콜라이더가 여러 개여도 같은 대상은 한 번만 검사
    readonly HashSet<IInteractable> _checkedTargets = new HashSet<IInteractable>();
    void Update()
    {
        //일시정지하거나 메뉴 사용 중이면 대상 선택 해제
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
        {
            UpdateCurrentTarget(null);
            _nextTargetCheckTime = 0f;
            return;
        }
        //엄폐 중에는 E키로 엄폐 해제만 가능
        if(_playerCover != null && _playerCover.IsInCover)
        {
            UpdateCurrentTarget(null);
            _nextTargetCheckTime = 0f;
            if (Input.GetKeyDown(KeyCode.E))
            {
                _playerCover.ExitCover();
            }
            return;
        }

        bool interactPressed = Input.GetKeyDown(KeyCode.E);
        //일정 간격으로 탐색하고, E키를 누른 순간에는 즉시 재검사
        if(interactPressed || Time.time >= _nextTargetCheckTime)
        {
            UpdateCurrentTarget(FindTarget());
            _nextTargetCheckTime = Time.time + _targetCheckInterval;
        }
        
        Component targetComponent = _currentTarget as Component;

        if (interactPressed && targetComponent != null && _currentTarget.CanInteract)
        { 
            _currentTarget.Interact();
        }
    }
    //현재 상태에 맞는 화면 안내 문구 반환
    public string GetInteractionPrompt()
    {
        if(!isActiveAndEnabled || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
        {
            return string.Empty;
        }

        if(_playerCover != null && _playerCover.IsInCover)
        {
            return "[E] 엄폐 해제";
        }

        Component targetComponent = _currentTarget as Component;

        if(targetComponent == null || !_currentTarget.CanInteract)
        {
            return string.Empty;
        }

        string displayName = _currentTarget.DisplayName;
        
        if(_cachedPrompt == null || _cachedDisplayName != displayName)
        {
            _cachedDisplayName = displayName;
            _cachedPrompt = $"<color=#C7AA70>[E]</color> {displayName}";
        }

        return _cachedPrompt;
    }
    void UpdateCurrentTarget(IInteractable target)
    {
        _currentTarget = target;
    }

    IInteractable FindTarget()
    {
        //동전 -> 문, 라디오 -> 일반 대상 -> 엄폐 순으로 우선 선택
        int GetInteractionPriority(IInteractable target)
        {
            if (target is ThrowableCoin) return 3;
            if(target is DoorController || target is NoiseDevice) return 2;
            if (target is CoverWall) return 0;
            return 1;
        }
        if (_playerCamera == null || _interactionOrigin == null) return null;

        Vector3 origin = _interactionOrigin.position;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        float sqrInteractionDistance = _interactionDistance * _interactionDistance;

        int count = Physics.OverlapSphereNonAlloc(origin, _interactionDistance, _overlapBuffer, _rayMask, QueryTriggerInteraction.Ignore);
        Collider[] colliders = _overlapBuffer;

        //배열이 가득 차면 일반 검색으로 다시 확인해 대상 누락 방지
        if(count == _overlapBuffer.Length)
        {
            colliders = Physics.OverlapSphere(origin, _interactionDistance, _rayMask, QueryTriggerInteraction.Ignore);
            count = colliders.Length;
        }

        _checkedTargets.Clear();

        IInteractable bestTarget= null;
        float bestScore = float.PositiveInfinity;
        int bestPriority = -1;

        for(int i = 0; i < count; i++) 
        {
            Collider col = colliders[i];
            //콜라이더 자신이나 부모에서 상호작용 기능 찾기
            IInteractable target = col.GetComponentInParent<IInteractable>();
            
            if(target == null) continue;    

            if (!_checkedTargets.Add(target)) continue;
            if (!target.CanInteract) continue;

            Vector3 point = target.InteractionPosition;

            //실제 상호작용 위치가 거리와 정면 범위 안에 있는지 확인
            float sqrDistance = (point - origin).sqrMagnitude;

            if (sqrDistance > sqrInteractionDistance) continue;
            if(!IsInFront(origin,point,flatForward)) continue;
            Vector3 viewport = _playerCamera.WorldToViewportPoint(point);

            if(!IsInsideCameraView(viewport)) continue;

            //플레이어 또는 카메라에서 대상이 가려지면 제외
            if (IsBlocked(origin,point,target)) continue;
            if (IsBlocked(_playerCamera.transform.position, point, target)) continue;
            
            //화면 중앙에 가까울수록 점수가 작음
            float x = (viewport.x - 0.5f) * _playerCamera.aspect;
            float y = viewport.y - 0.5f;
            float score = x * x + y * y;
            //우선순위가 같으면 화면 중앙에 더 가까운 대상 선택
            int priority = GetInteractionPriority(target);
            if(bestTarget == null || priority > bestPriority || (priority == bestPriority && score < bestScore))
            {
                bestScore = score;
                bestTarget = target;
                bestPriority = priority;
            }
        }
        return bestTarget;

    }

    bool IsInFront(Vector3 origin, Vector3 point, Vector3 flatForward)
    {
        //높이 차이를 제외하고 좌우 각도 확인
        Vector3 flatToTarget = Vector3.ProjectOnPlane(point - origin, Vector3.up);

        if (flatToTarget.sqrMagnitude <= 0.001f) return true;

        float angle = Vector3.Angle(flatForward,flatToTarget);

        return angle <= _interactionAngle * 0.5f;
    }
    //대상이 카메라의 표시 범위 안에 있는지 확인
    bool IsInsideCameraView(Vector3 viewport)
    {
        //x,y가 0~1 안이면 화면 안쪽 
        return viewport.z >= _playerCamera.nearClipPlane && viewport.z <= _playerCamera.farClipPlane && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }
    bool IsBlocked(Vector3 origin, Vector3 point, IInteractable target)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;

        if(distance <= 0.001f) return false;
        //대상까지 직선을 쏴서 가로막는 물체 확인
        bool hasHit = Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance, _rayMask, QueryTriggerInteraction.Ignore);
        //중간에 부딪힌 Collider가 없으면 가려지지 않음
        if(!hasHit) return false;
        
        IInteractable hitTarget = hit.collider.GetComponentInParent<IInteractable>();

        //대상 자신에 맞은 것은 허용하고, 다른 물체에 맞으면 차단
        return !ReferenceEquals(hitTarget, target);
    }

     void OnDisable()
    {
        //비활성화되면 이전 대상 정보 초기화
        _currentTarget = null;
        _checkedTargets.Clear();
        _nextTargetCheckTime = 0f;
    }

}
