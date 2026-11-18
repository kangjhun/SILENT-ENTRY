using System;
using UnityEngine;
//던진 동전의 충돌 소음과 회수 처리
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public class ThrowableCoin : MonoBehaviour,IInteractable
{
    //동전 소리가 발생했을 때 연결된 함수에 위치와 범위 전달
    public static event Action<Vector3, float> NoiseEmitted;

    [SerializeField, Min(0.1f)] float _noiseRadius = 6f;

    [SerializeField, Min(0f)] float _pickupDelay = 0.4f;
    [SerializeField, Min(0f)] float _pickupMaxSpeed = 0.2f;

    Rigidbody _rigidbody;
    MeshCollider _collider;
    PlayerCoinThrower _owner;

    bool _hasImpacted;
    bool _collected;
    float _firstImpactTime;
    //미리보기에서도 실제 동전과 같은 소음 반경을 사용
    public float NoiseRadius => _noiseRadius;
    public string DisplayName => "동전 회수";

    public Vector3 InteractionPosition => _collider != null ? _collider.bounds.center : transform.position;
    //첫 충돌 후 일정 시간이 지나고 거의 멈췄을 때 회수 가능
    public bool CanInteract => isActiveAndEnabled && !_collected && _owner != null && _owner.CanRecoverCoin && _hasImpacted &&
        Time.time >= _firstImpactTime + _pickupDelay && _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude <= _pickupMaxSpeed * _pickupMaxSpeed;
    //게임 실행 시작 시 이전 이벤트 연결 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents()
    {
        NoiseEmitted = null;
    }
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<MeshCollider>();
    }

    public void Initialize(PlayerCoinThrower owner,Vector3 launchVelocity, Collider[] playerColliders)
    {
        
        _owner = owner;
        //던진 동전이 플레이어 몸에 부딪히지 않도록 설정
        if(playerColliders != null)
        {
            foreach(Collider playerCollider in playerColliders)
            {
                if(playerCollider == null) continue;
                Physics.IgnoreCollision(_collider, playerCollider, true);
            }
        }
        //전달받은 속도로 동전 발사
        _rigidbody.AddForce(launchVelocity, ForceMode.VelocityChange);
    }

    void OnCollisionEnter(Collision collision)
    {
        //첫 충돌에서만 소음 발생
        if (_owner == null || _hasImpacted || _collected) return;

        _hasImpacted = true;
        _firstImpactTime = Time.time;

        Vector3 noisePosition = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        //소음 이벤트에 연결된 함수 실행
        NoiseEmitted?.Invoke(noisePosition, _noiseRadius);
    }

    public void Interact()
    {
        if(!CanInteract) return;
        if(!_owner.TryRecoverCoin()) return;
        //회수에 성공하면 중복 회수를 막고 동전 제거
        _collected = true;
        _collider.enabled = false;
        Destroy(gameObject);
    }
    
}
