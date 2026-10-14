using UnityEngine;

[RequireComponent(typeof(EnemyFSM))]
public class EnemyCoinHearing : MonoBehaviour
{
    EnemyFSM _enemy;

     void Awake()
    {
        _enemy = GetComponent<EnemyFSM>();
    }

     void OnEnable()
    {
        ThrowableCoin.NoiseEmitted += HandleCoinNoise;
    }

     void OnDisable()
    {
        ThrowableCoin.NoiseEmitted -= HandleCoinNoise;
    }

    void HandleCoinNoise(Vector3 position, float radius)
    {
        if(_enemy == null || !_enemy.isActiveAndEnabled) return;

        _enemy.HearNoise(position, radius);
    }
}
