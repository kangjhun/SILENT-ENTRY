using UnityEngine;
//목표물 획득 상호작용 처리
public class ObjectiveItem : MonoBehaviour,IInteractable
{
    [SerializeField] GameManager _gameManager;

    [Header("상호작용 정보")]
    [SerializeField] string _displayName = "목표물 노트북";
    [SerializeField] Transform _interactionPoint;

    //화면 안내에 표시할 이름
    public string DisplayName => _displayName;

    //지정된 기준점이 없으면 오브젝트 위치 사용
    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    // 게임 진행 중이고 아직 획득하지 않은 목표물만 상호작용 가능
    public bool CanInteract => isActiveAndEnabled && _gameManager != null && _gameManager.IsPlaying && !_gameManager.HasObjective;

    public void Interact()
    {
        if(!CanInteract) { return; }

        //획득 처리에 성공하면 목표물 숨기기
        if (_gameManager.AcquiredObjective())
        {
            gameObject.SetActive(false);    
        }
    }
}
