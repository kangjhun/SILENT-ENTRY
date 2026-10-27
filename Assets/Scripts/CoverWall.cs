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

    public Vector3 InteractionPosition => _playerCover != null ? _wallCollider.ClosestPoint(_playerCover.ProbePosition) : transform.position;

    public bool CanInteract
    {
        get
        {
            if(!isActiveAndEnabled || _wallCollider == null || !_wallCollider.enabled || _playerCover == null || !_playerCover.CanEnter)
            {
                return false;
            }

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

        _playerCover.EnterCover(_wallCollider);
    }
}
