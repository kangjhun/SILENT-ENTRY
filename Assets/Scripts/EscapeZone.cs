using UnityEngine;
//상호작용으로 탈출을 시도하는 구역
[RequireComponent(typeof(BoxCollider))]
public class EscapeZone : MonoBehaviour, IInteractable
{
    [Header("게임 진행 관리")]
    [SerializeField] GameManager _gameManager;

    [Header("상호작용 기준점")]
    [SerializeField] Transform _interactionPoint;

    BoxCollider _doorCollider;

    public string DisplayName => "탈출하기";
    //지정된 기준점이 없으면 콜라이더 중심 사용
    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : _doorCollider.bounds.center;
    //필수 연결과 활성 상태 확인
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
        //GameManager에서 탈출 조건 확인 및 결과 처리
        _gameManager.TryEscape();
    }
}
