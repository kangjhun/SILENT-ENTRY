using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public class ThrowableCoin : MonoBehaviour,IInteractable
{
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

    public string DisplayName => "동전 회수";

    public Vector3 InteractionPosition => _collider != null ? _collider.bounds.center : transform.position;

    public bool CanInteract => isActiveAndEnabled && !_collected && _owner != null && _owner.CanRecoverCoin && _hasImpacted &&
        Time.time >= _firstImpactTime + _pickupDelay && _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude <= _pickupMaxSpeed * _pickupMaxSpeed;

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
        if(playerColliders != null)
        {
            foreach(Collider playerCollider in playerColliders)
            {
                if(playerCollider == null) continue;
                Physics.IgnoreCollision(_collider, playerCollider, true);
            }
        }

        _rigidbody.AddForce(launchVelocity, ForceMode.VelocityChange);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_owner == null || _hasImpacted || _collected) return;

        _hasImpacted = true;
        _firstImpactTime = Time.time;

        Vector3 noisePosition = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        Debug.Log($"동전 소음 발생 / 빈걍 : {_noiseRadius}", this);

        NoiseEmitted?.Invoke(noisePosition, _noiseRadius);
    }

    public void Interact()
    {
        if(!CanInteract) return;
        if(!_owner.TryRecoverCoin()) return;
        _collected = true;
        _collider.enabled = false;
        Destroy(gameObject);
    }
    
}
