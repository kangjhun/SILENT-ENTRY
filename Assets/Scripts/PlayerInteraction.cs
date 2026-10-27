using System.Collections.Generic;
using UnityEngine;
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

    IInteractable _currentTarget;

    //한 대상에 Collider가 여러 개 있어도 한 번만 검사
    readonly HashSet<IInteractable> _checkedTargets = new HashSet<IInteractable>();
    void Update()
    {
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
        {
            UpdateCurrentTarget(null);
            return;
        }

        if(_playerCover != null && _playerCover.IsInCover)
        {
            UpdateCurrentTarget(null);
            if (Input.GetKeyDown(KeyCode.E))
            {
                _playerCover.ExitCover();
            }
            return;
        }
        //매 프레임 현재 조건에 맞는 대상 선택
        IInteractable target = FindTarget();

        UpdateCurrentTarget(target);

        //대상이 있고 현재 상호작용 가능한 경우에만 실행
        if(Input.GetKeyDown(KeyCode.E) && _currentTarget != null && _currentTarget.CanInteract)
        {
            
            _currentTarget.Interact();
        }
    }

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

        return $"<color=#C7AA70>[E]</color>  {_currentTarget.DisplayName}"; 
    }
    void UpdateCurrentTarget(IInteractable target)
    {
        _currentTarget = target;
    }

    IInteractable FindTarget()
    {
        int GetInteractionPriority(IInteractable target)
        {
            if (target is ThrowableCoin) return 3;
            if(target is DoorController || target is NoiseDevice) return 2;
            if (target is CoverWall) return 0;
            return 1;
        }
        if (_playerCamera == null || _interactionOrigin == null) return null;

        Vector3 origin = _interactionOrigin.position;

        //모든 후보에게 공통으로 사용하는 값은 한 번만 계산
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        float sqrInteractionDistance = _interactionDistance * _interactionDistance;

        //기준점 주변의 Collider를 검색 Trigger Collider는 대상에서 제외
        Collider[] colliders = Physics.OverlapSphere(origin, _interactionDistance, _rayMask, QueryTriggerInteraction.Ignore);
        _checkedTargets.Clear();

        IInteractable bestTarget= null;
        float bestScore = float.PositiveInfinity;
        int bestPriority = -1;

        foreach(Collider col in colliders)
        {
            //Collider 자신이나 부모에 있는 상호작용 컴포넌트 검색
            IInteractable target = col.GetComponentInParent<IInteractable>();
            
            if(target == null) continue;    

            //이미 검사한 대상이면 건너뜀
            if (!_checkedTargets.Add(target)) continue;

           
            if (!target.CanInteract) continue;

            Vector3 point = target.InteractionPosition;

            //Collider 일부가 범위에 걸쳐 있어도,
            //실제 상호작용 기준점이 너무 멀면 제왼
            float sqrDistance = (point - origin).sqrMagnitude;

            if (sqrDistance > sqrInteractionDistance) continue;

            //캐릭터가 바라보는 수평 방향을 기준으로 판정
            if(!IsInFront(origin,point,flatForward)) continue;

            //월드 좌표를 카메라 화면 기주 좌표로 변환
            Vector3 viewport = _playerCamera.WorldToViewportPoint(point);

            if(!IsInsideCameraView(viewport)) continue;

            //플레이어와 대상 사이에 장애물이 있으면 제외
            if (IsBlocked(origin,point,target)) continue;

            //카메라에서 대상이 가려져 있어도 제외
            if (IsBlocked(_playerCamera.transform.position, point, target)) continue;
            
            //화면 중앙에 가까운 대상일수록 작은 점수
            //가로 좌표에는 화면 종횡비를 반영
            float x = (viewport.x - 0.5f) * _playerCamera.aspect;
            float y = viewport.y - 0.5f;
            float score = x * x + y * y;

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
        //높이 차이를 제외하고 앞뒤 방향만 비교
        Vector3 flatToTarget = Vector3.ProjectOnPlane(point - origin, Vector3.up);

        //수평 위치가 거의 같으면 방향 제한을 적용하지 않음
        if (flatToTarget.sqrMagnitude <= 0.001f) return true;

        float angle = Vector3.Angle(flatForward,flatToTarget);

        //전체 허용 각도의 절반을 좌우에 적용
        return angle <= _interactionAngle * 0.5f;
    }

    bool IsInsideCameraView(Vector3 viewport)
    {
        //x,y가 0~1 안이면 화면 안쪽 
        return viewport.z >= _playerCamera.nearClipPlane && viewport.z <= _playerCamera.farClipPlane && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }
    bool IsBlocked(Vector3 origin, Vector3 point, IInteractable target)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;

        //두 지점이 사실상 같으면 가림 검사 생략
        if(distance <= 0.001f) return false;

        bool hasHit = Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance, _rayMask, QueryTriggerInteraction.Ignore);
        //중간에 부딪힌 Collider가 없으면 가려지지 않음
        if(!hasHit) return false;
        
        IInteractable hitTarget = hit.collider.GetComponentInParent<IInteractable>();

        //대상 자신의 Collider라면 허용
        //벽이나 다른 상호작용 대상이라면 가려진 것으로 판단
        return !ReferenceEquals(hitTarget, target);
    }

     void OnDisable()
    {
        //조작 중단 시 이전 대상 정보 처리
        _currentTarget = null;
        _checkedTargets.Clear();
    }

}
