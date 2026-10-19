using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EscapeZone : MonoBehaviour, IInteractable
{
    [Header("게임 진행 관리")]
    [SerializeField] GameManager _gameManager;

    [Header("상호작용 기준점")]
    [SerializeField] Transform _interactionPoint;

    BoxCollider _doorCollider;

    public string DisplayName => "탈출하기";

    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : _doorCollider.bounds.center;

    public bool CanInteract => isActiveAndEnabled && _gameManager != null && _doorCollider != null && _doorCollider.enabled;
    

     void Awake()
    {
        _doorCollider = GetComponent<BoxCollider>();
        _doorCollider.isTrigger = false;

        if(_gameManager == null)
        {
            Debug.LogError("GameManager를 연결하세요.", this);
            enabled = false;
        }
    }
    

     public void Interact()
    {
        if (!CanInteract) return;
        _gameManager.TryEscape();
    }
}
