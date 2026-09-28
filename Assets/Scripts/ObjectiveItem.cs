using UnityEngine;

public class ObjectiveItem : MonoBehaviour,IInteractable
{
    [SerializeField] GameManager _gameManager;

    [Header("상호작용 정보")]
    [SerializeField] string _displayName = "목표물 노트북";
    [SerializeField] Transform _interactionPoint;

    //상호작용 안내에 표시할 이름
    public string DisplayName => _displayName;

    //거리, 방향, 가림 판정에 사용할 월드 좌표
    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    //활성화된 목표물이고, 게임 진행 중이며, 아직 목표물을 획득하지 않았을 때만 상호작용 허용
    public bool CanInteract => isActiveAndEnabled && _gameManager != null && _gameManager.IsPlaying && !_gameManager.HasObjective;

    public void Interact()
    {
        //다른 코드에서 직접 호출해도 조건을 다시 확인
        if(!CanInteract) { return; }

        //GameManager가 획득 처리에 성공했을 때만 숨김
        if (_gameManager.AcquiredObjective())
        {
            gameObject.SetActive(false);    
        }
    }
}
