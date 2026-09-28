using UnityEngine;

public interface IInteractable
{
    //안내에 표시할 이름
    string DisplayName { get; }
    //거리, 방향, 가림 여부를 검사할 월드 좌표
    Vector3 InteractionPosition {  get; }
    //현재 상호작용할 수 있는 상태인지
    bool CanInteract {  get; }
    //상호작용 입력을 받았을 때 수행할 동작
    void Interact();

}
