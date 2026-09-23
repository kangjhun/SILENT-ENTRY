using UnityEngine;

public class NoiseDevice : MonoBehaviour,IInteractable
{
    [Header("상호작용")]
    [SerializeField] string _displayName = "라디오";
    [SerializeField] Transform _interactionPoint;

    [Header("소음을 들을 적")]
    [SerializeField] EnemyFSM[] _enemies;

    [Header("소음 설정")]
    [SerializeField, Min(0.1f)] float _hearingRadius = 10f;
    [SerializeField, Min(0f)] float _cooldown = 2f;

    float _nextUseTime;

    public string DisplayName => _displayName;

    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    public bool CanInteract => isActiveAndEnabled;
    public void Interact()
    {
        if (!CanInteract) return;

        if (Time.time < _nextUseTime) return;

        _nextUseTime = Time.time + _cooldown;

        Debug.Log($"{_displayName} 소음 발생", this);

        if (_enemies == null) return;

        foreach(EnemyFSM enemy in _enemies)
        {
            if (enemy == null) continue;

            enemy.HearNoise(transform.position,_hearingRadius);
        }
    }
}
