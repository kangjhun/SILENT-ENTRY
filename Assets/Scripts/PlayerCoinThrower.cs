using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCoinThrower : MonoBehaviour
{
    [SerializeField] Camera _playerCamera;
    [SerializeField] Transform _throwOrigin;
    [SerializeField] ThrowableCoin _coinPrefab;
    [SerializeField] PlayerCover _playerCover;
    [SerializeField] Animator _animator;

    [SerializeField, Min(1f)] float _throwAnimationTimeout = 5f;

    [SerializeField, Min(1)] int _maxCoins = 3;

    [SerializeField, Min(0.1f)] float _minThrowSpeed = 3f;
    [SerializeField, Min(0.1f)] float _maxThrowSpeed = 12f;

    [SerializeField, Min(0.1f)] float _throwCooldown = 0.4f;

    [SerializeField, Min(0f)] float _upwardSpeed = 2f;
    [SerializeField, Min(0.1f)] float _fullChargeTime = 1.5f;

    [SerializeField] GameObject _chargeGaugeRoot;
    [SerializeField] Slider _chargeSlider;

    [SerializeField] LayerMask _environmentMask;
    [SerializeField, Min(0.01f)] float _spawnCheckRadius = 0.09f;

    bool _isCharging;
    bool _isThrowing;
    bool _releasePending;

    float _pendingPower;
    float _throwTimeoutAt;
    float _chargeTime;

    Collider[] _playerColliders;
    float _nextThrowTime;

    static readonly int ThrowCoinHash = Animator.StringToHash("ThrowCoin");
    public int CoinCount { get; private set; }
    public int MaxCoins => _maxCoins;
    public bool CanRecoverCoin => CoinCount < _maxCoins;
    public float ChargeRatio => Mathf.Clamp01(_chargeTime / Mathf.Max(_fullChargeTime, 0.01f));

    void Awake()
    {
        CoinCount = _maxCoins;
        _playerColliders = GetComponentsInChildren<Collider>();
        PrintCount();

        if(_chargeSlider != null)
        {
            _chargeSlider.minValue = 0f;
            _chargeSlider.maxValue = 1f;
            _chargeSlider.wholeNumbers = false;
            _chargeSlider.interactable = false;
        }
        CancelCharge();
    }

     void Update()
    {
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked || (_playerCover != null && _playerCover.IsInCover))
        {
            CancelCharge();
            CancelThrowAnimation();
            return; 
        }

        if (_isThrowing)
        {
            if(Time.time >= _throwTimeoutAt)
            {
                Debug.LogWarning("투척 애니메이션 이벤트와 전이를 확인", this);
                CancelThrowAnimation();
            }
            return;
        }
  
        if (Input.GetKeyDown(KeyCode.G) && !_isCharging)
        {
            if (Time.time < _nextThrowTime) return;

            if(CoinCount <= 0)
            {
                Debug.Log("던질 동전이 없습니다.", this);
                return;
            }
            if(_playerCamera == null || _throwOrigin == null || _coinPrefab == null)
            {
                Debug.LogWarning("동전 투척에 필요한 연결을 확인하세요.", this);
                return;
            }

            _isCharging = true;
            _chargeTime = 0f;

            if (_chargeGaugeRoot != null)
                _chargeGaugeRoot.SetActive(true);

            UpdateChargeGauge();
        }

        if(!_isCharging) { return; }
        if (Input.GetKey(KeyCode.G))
        {
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, _fullChargeTime);
            UpdateChargeGauge();
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            float power = ChargeRatio;
            CancelCharge();
            BeginThrowAnimation(power);
        }else if (!Input.GetKey(KeyCode.G))
        {
            CancelCharge();
        }
    }
    void UpdateChargeGauge()
    {
        if (_chargeSlider != null)
            _chargeSlider.value = ChargeRatio;
    }

    void CancelCharge()
    {
        _isCharging = false;
        _chargeTime = 0f;

        if(_chargeSlider !=  null)
            _chargeSlider.value = 0f;

        if(_chargeGaugeRoot != null)
            _chargeGaugeRoot.SetActive(false);
    }

     void OnDisable()
    {
        CancelCharge() ;
        CancelThrowAnimation();
    }

     void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelCharge();
            CancelThrowAnimation() ;
        }
    }
    void TryThrow(float chargeRatio)
    {
        if (Time.time < _nextThrowTime) return;

        if (_playerCamera == null || _throwOrigin == null || _coinPrefab == null)
        {
            Debug.LogWarning("동전 투척에 필요한 연결을 확인하세요.", this);
            return;
        }

        if (CoinCount <= 0)
        {
            Debug.Log("던질 동전이 없습니다.", this);
            return;
        }

        Vector3 origin = _throwOrigin.position;

        if (Physics.CheckSphere(origin, _spawnCheckRadius, _environmentMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("장애물에 너무 가까워 던질 수 없습니다.", this);
            return;
        }

        Ray aimRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = aimRay.GetPoint(15f);

        if(Physics.Raycast(aimRay,out RaycastHit hit, 15f, _environmentMask, QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
        }

        Vector3 direction = (aimPoint - origin).normalized;

        float throwSpeed = Mathf.Lerp(_minThrowSpeed, _maxThrowSpeed, Mathf.Clamp01(chargeRatio));

        Vector3 launchVelocity = direction * throwSpeed + Vector3.up * _upwardSpeed;

        ThrowableCoin coin = Instantiate(_coinPrefab, origin, Quaternion.identity);

        coin.Initialize(this,launchVelocity,_playerColliders);

        CoinCount--;
        _nextThrowTime = Time.time + _throwCooldown;

        PrintCount();
    }

    public bool TryRecoverCoin()
    {
        if(!CanRecoverCoin) return false;
        CoinCount++;
        PrintCount();
        return true;
    }

    void PrintCount()
    {
        Debug.Log($"동전 : {CoinCount}/{_maxCoins}", this);
    }

    void BeginThrowAnimation(float power)
    {
        if(_isThrowing || CoinCount <= 0) return;
        if (Time.time < _nextThrowTime) return;

        if(_animator == null || !_animator.isActiveAndEnabled)
        {
            Debug.LogWarning("투척에 사용할 Animator를 연결하세요", this);
            return;
        }

        _pendingPower = power;
        _releasePending = true;
        _isThrowing = true;

        _throwTimeoutAt = Time.time + _throwAnimationTimeout;

        _animator.ResetTrigger(ThrowCoinHash);
        _animator.SetTrigger(ThrowCoinHash);
    }

    public void ReleaseCoin()
    {
        if(!isActiveAndEnabled || !_isThrowing || !_releasePending) return;

        _releasePending = false;

        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked || (_playerCover != null && _playerCover.IsInCover))
            return;
        TryThrow(_pendingPower);
    }

    public void FinishCoinThrow()
    {
        _isThrowing = false;
        _releasePending = false;
    }

    void CancelThrowAnimation()
    {
        bool wasThrowing = _isThrowing;
        _isThrowing = false;
        _releasePending = false;

        if (_animator == null) return;
        _animator.ResetTrigger(ThrowCoinHash);

        if(wasThrowing && _animator.isActiveAndEnabled)
        {
            int layerIndex = _animator.GetLayerIndex("CoinThrow");
            if(layerIndex >= 0)
            {
                _animator.Play("CoinThrow.Empty", layerIndex, 0f);
            }
        }
    }
}
