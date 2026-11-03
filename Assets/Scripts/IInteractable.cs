using UnityEngine;
//상호작용 가능한 오브젝트가 공통으로 갖춰야 할 기능
public interface IInteractable
{
    //화면 안내에 표시할 이름
    string DisplayName { get; }
    //거리, 방향, 장애물 확인에 사용할 위치 
    Vector3 InteractionPosition {  get; }
    //현재 상호작용할 수 있는지 확인
    bool CanInteract {  get; }
    //상호작용 시 실행할 동작
    void Interact();

}
