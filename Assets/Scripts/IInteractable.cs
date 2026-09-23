using UnityEngine;

public interface IInteractable
{
    string DisplayName { get; }
    Vector3 InteractionPosition {  get; }
    bool CanInteract {  get; }
    void Interact();

}
