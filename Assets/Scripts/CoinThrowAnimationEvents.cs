using UnityEngine;

//애니메이션 이벤트를 동전 던지기 기능에 전달
public class CoinThrowAnimationEvents : MonoBehaviour
{
    [SerializeField] PlayerCoinThrower _coinThrower;

    //동전이 손에서 떨어지는 시점
    public void OnCoinRelease()
    {
        if (_coinThrower != null)
            _coinThrower.ReleaseCoin();
    }

    //던지는 애니메이션이 끝나는 시점
    public void OnCoinThrowFinished()
    {
        if (_coinThrower != null)
            _coinThrower.FinishCoinThrow();
    }
}
