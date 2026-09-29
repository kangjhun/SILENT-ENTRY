using UnityEngine;

[RequireComponent (typeof(BoxCollider))]
public class CoverWall : MonoBehaviour, IInteractable
{
    [SerializeField] PlayerCover _playerCover;

    BoxCollider _wallCollider;

    public string DisplayName => "벽에 기대기";

    public Vector3 InteractionPosition => _playerCover != null ? _wallCollider.ClosestPoint(_playerCover.ProbePosition) : transform.position;

    public bool CanInteract => isActiveAndEnabled && _wallCollider.enabled && _playerCover != null && _playerCover.CanEnter;

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
