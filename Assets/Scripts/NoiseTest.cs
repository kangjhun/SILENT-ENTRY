using UnityEngine;

public class NoiseTest : MonoBehaviour
{
    [SerializeField] EnemyFSM _enemy;
    [SerializeField] float _hearingRadius = 10f;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.N) && _enemy != null)
        {
            _enemy.HearNoise(transform.position, _hearingRadius);
        }
    }
}
