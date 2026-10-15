using UnityEngine;

public class CoinThrowAnimationEvents : MonoBehaviour
{
    [SerializeField] PlayerCoinThrower _coinThrower;
    public void OnCoinRelease()
    {
        if (_coinThrower != null)
            _coinThrower.ReleaseCoin();
    }

    public void OnCoinThrowFinished()
    {
        if (_coinThrower != null)
            _coinThrower.FinishCoinThrow();
    }
}
