using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] float _blendTime = 0.1f;

    PlayerMovement _playerMovement;
   
     void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (_animator == null)
        {
            Debug.LogError("animator를 연결해주세요", this);
            enabled = false;
        }
    }

    void LateUpdate()
    {
        Vector2 move =_playerMovement.AnimationMove;

        _animator.SetFloat("MoveX",move.x,_blendTime,Time.deltaTime);
        _animator.SetFloat("MoveY",move.y,_blendTime,Time.deltaTime);
        _animator.SetBool("IsRunning",_playerMovement.IsRunning );
    }
}
