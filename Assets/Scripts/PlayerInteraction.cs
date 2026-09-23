using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] Camera _playerCamera;

    [Header("플레이어 가슴 높이의 기준점")]
    [SerializeField] Transform _interactionOrigin;

    [Header("상호작용 설정")]
    [SerializeField, Min(0.1f)] float _interactionDistance = 2f;

    [Header("캐릭터 정면 판정")]
    [SerializeField, Range(1f, 180f)]
    float _interactionAngle = 180f;

    [SerializeField] LayerMask _rayMask;

    IInteractable _currentTarget;

    readonly HashSet<IInteractable> _checkedTargets = new HashSet<IInteractable>();
    void Update()
    {
        IInteractable target = FindTarget();
        if(!ReferenceEquals(target,_currentTarget))
        {
            _currentTarget = target;

            if(_currentTarget != null)
            {
                Debug.Log($"E : {_currentTarget.DisplayName}작동", this);
            } else
            {
                Debug.Log("상호작용 대상 없음", this);
            }
        }

        if(Input.GetKeyDown(KeyCode.E) && _currentTarget != null && _currentTarget.CanInteract)
        {
            _currentTarget.Interact();
        }
    }

    IInteractable FindTarget()
    {
        if (_playerCamera == null || _interactionOrigin == null) return null;

        Vector3 origin = _interactionOrigin.position;

        Collider[] colliders = Physics.OverlapSphere(origin, _interactionDistance, _rayMask, QueryTriggerInteraction.Ignore);
        _checkedTargets.Clear();

        IInteractable bestTarget= null;
        float bestScore = float.PositiveInfinity;

        foreach(Collider col in colliders)
        {
            IInteractable target = col.GetComponentInParent<IInteractable>();
            
            if(target == null) continue;    

            if (!_checkedTargets.Add(target)) continue;

            if(!target.CanInteract) continue;

            Vector3 point = target.InteractionPosition;

            float sqrDistance = (point - origin).sqrMagnitude;

            if (sqrDistance > _interactionDistance * _interactionDistance) continue;

            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            Vector3 flatToTarget = Vector3.ProjectOnPlane(point - origin, Vector3.up);

            if(flatToTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(flatForward, flatToTarget);
                if(angle > _interactionAngle * 0.5f) continue;
            }


            Vector3 viewport = _playerCamera.WorldToViewportPoint(point);

            if (viewport.z < _playerCamera.nearClipPlane || viewport.z > _playerCamera.farClipPlane || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                continue;

            if (IsBlocked(origin,point,target)) continue;

            if (IsBlocked(_playerCamera.transform.position, point, target)) continue;
            
            float x = (viewport.x - 0.5f) * _playerCamera.aspect;
            float y = viewport.y - 0.5f;
            float score = x * x + y * y;

            if(score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }
        return bestTarget;

    }
    bool IsBlocked(Vector3 origin, Vector3 point, IInteractable target)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;

        if(distance <= 0.001f) return false;

        if(Physics.Raycast(origin,direction/distance,out RaycastHit hit, distance, _rayMask, QueryTriggerInteraction.Ignore))
        {
            IInteractable hitTarget = hit.collider.GetComponentInParent<IInteractable>();
            return hitTarget != target;
        }
        return false;
    }

     void OnDisable()
    {
        _currentTarget = null;
        _checkedTargets.Clear();
    }

}
