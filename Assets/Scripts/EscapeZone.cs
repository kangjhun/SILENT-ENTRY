using UnityEngine;

[RequireComponent(typeof(BoxCollider),typeof(Rigidbody))]
public class EscapeZone : MonoBehaviour
{
    [Header("게임 진행 관리")]
    [SerializeField] GameManager _gameManager;

    [Header("탈출 가능한 플레이어")]
    [SerializeField] PlayerMovement _player;
     void Awake()
    {
        //플레이어를 막는 벽이 아닌 진입 감지 영역으로 사용
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        
        //물리 힘이나 중력으로 탈출 구역이 움직이지 않도록 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
    void Start()
    {
        if(_gameManager == null || _player == null)
        {
            Debug.LogError("GameManager와 Player를 연결하세요.",this);
            enabled = false;
        }
    }

     void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled) return;

        if (_gameManager == null || _player == null) return;

        //진입한 Collider 자신과 부모에서 플레이어 컴포넌트 검색
        PlayerMovement enteredPlayer = other.GetComponentInParent<PlayerMovement>();

        //지정한 플레이어만 탈출 가능
        if (enteredPlayer != _player) return;

        //목표물 보유 여부와 게임 진행 상태는 GameManager에서 판정
        _gameManager.TryEscape();
    }
}
