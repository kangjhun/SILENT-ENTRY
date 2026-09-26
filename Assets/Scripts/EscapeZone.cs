using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] PlayerMovement _player;
     void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
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

        PlayerMovement enteredPlayer = other.GetComponentInParent<PlayerMovement>();

        if (enteredPlayer != _player) return;

        _gameManager.TryEscape();
    }
}
