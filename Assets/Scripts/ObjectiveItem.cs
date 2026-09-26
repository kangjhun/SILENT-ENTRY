using UnityEngine;

public class ObjectiveItem : MonoBehaviour,IInteractable
{
    [SerializeField] GameManager _gameManager;

    [SerializeField] string _displayName = "목표물 노트북";
    [SerializeField] Transform _interactionPoint;

    public string DisplayName => _displayName;

    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    public bool CanInteract => isActiveAndEnabled && _gameManager != null && _gameManager.IsPlaying && !_gameManager.HasObjective;

    public void Interact()
    {
        if(!CanInteract) { return; }

        if (_gameManager.AcquiredObjective())
        {
            gameObject.SetActive(false);    
        }
    }
}
