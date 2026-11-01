using UnityEngine;
//동전 소음을 감지해 적 AI에 전달
[RequireComponent(typeof(EnemyFSM))]
public class EnemyCoinHearing : MonoBehaviour
{
    EnemyFSM _enemy;

     void Awake()
    {
        _enemy = GetComponent<EnemyFSM>();
    }

     void OnEnable()
    {//동전 소리가 발생하면 HandleCoinNoise가 실행되도록 연결
        ThrowableCoin.NoiseEmitted += HandleCoinNoise;
    }

     void OnDisable()
    {//비활성화되면 동전 소리 이벤트 연결 해제
        ThrowableCoin.NoiseEmitted -= HandleCoinNoise;
    }

    void HandleCoinNoise(Vector3 position, float radius)
    {
        if(_enemy == null || !_enemy.isActiveAndEnabled) return;
        //소음 위치와 감지 반경을 AI에 전달
        _enemy.HearNoise(position, radius);
    }
}
