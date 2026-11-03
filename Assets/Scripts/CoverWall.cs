using UnityEngine;

[RequireComponent (typeof(BoxCollider))]
public class CoverWall : MonoBehaviour, IInteractable
{
    [SerializeField] PlayerCover _playerCover;

    [Header("엄폐 상호작용 거리")]
    [SerializeField, Min(0.1f)]
    float _interactionDistance = 0.8f;

    BoxCollider _wallCollider;

    public string DisplayName => "벽에 기대기";

    //플레이어와 가장 가까운 벽 표면을 상호작용 위치로 사용
    public Vector3 InteractionPosition => _playerCover != null ? _wallCollider.ClosestPoint(_playerCover.ProbePosition) : transform.position;

    public bool CanInteract
    {
        get
        {
            //벽이 비활성 상태이거나 플레이어가 엄폐할 수 없으면 차단
            if(!isActiveAndEnabled || _wallCollider == null || !_wallCollider.enabled || _playerCover == null || !_playerCover.CanEnter)
            {
                return false;
            }
            //플레이어와 벽 사이의 거리가 상호작용 범위 안인지 확인
            Vector3 origin = _playerCover.ProbePosition;
            Vector3 closestPoint = _wallCollider.ClosestPoint(origin);
            float sqrDistance = (closestPoint - origin).sqrMagnitude;
            return sqrDistance <= _interactionDistance * _interactionDistance;
        }
    }

    void Awake()
    {
        _wallCollider = GetComponent<BoxCollider>();
    }

    public void Interact()
    {
        if (!CanInteract) return;
        //해당 벽에 엄폐
        _playerCover.EnterCover(_wallCollider);
    }
}
