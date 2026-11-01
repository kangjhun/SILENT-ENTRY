
using UnityEngine;
using UnityEngine.UI;
//동전 충전, 투척 애니메이션, 생성과 회수 처리
public class PlayerCoinThrower : MonoBehaviour
{
    [SerializeField] Camera _playerCamera;
    [SerializeField] Transform _throwOrigin;
    [SerializeField] Transform _throwSatfetyOrigin;
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

    [Header("화면 안내")]
    [SerializeField] GameUIManager _gameUIManager;  

    bool _isCharging;
    bool _isThrowing;
    bool _releasePending;//애니메이션에서 동전을 놓을 시점을 기다리는 중

    float _pendingPower;
    float _throwTimeoutAt;
    float _chargeTime;

    Collider[] _playerColliders;
    float _nextThrowTime;

    static readonly int ThrowCoinHash = Animator.StringToHash("ThrowCoin");
    public int CoinCount { get; private set; }
    public int MaxCoins => _maxCoins;
    public bool CanRecoverCoin => CoinCount < _maxCoins;
    //충전 시간을 0~1 범위의 값으로 변환
    public float ChargeRatio => Mathf.Clamp01(_chargeTime / Mathf.Max(_fullChargeTime, 0.01f));


    void ShowNotice(string message)
    {
        if (_gameUIManager != null)
            _gameUIManager.ShowNotice(message);
    }
    void Awake()
    {
        CoinCount = _maxCoins;
        _playerColliders = GetComponentsInChildren<Collider>();
        PrintCount();
        //충전 게이지를 표시용으로 설정
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
        //일시정지, 메뉴 사용, 엄폐 중에는 충전과 투척 취소
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked || (_playerCover != null && _playerCover.IsInCover))
        {
            CancelCharge();
            CancelThrowAnimation();
            return; 
        }
        //투척 중에는 추가 입력을 막고, 종료되지 않으면 시간 초과 처리
        if (_isThrowing)
        {
            if(Time.time >= _throwTimeoutAt)
            {
                Debug.LogWarning("투척 애니메이션 이벤트와 전이를 확인", this);
                CancelThrowAnimation();
            }
            return;
        }
        //G키를 누르면 투척 조건을 확인하고 충전 시작
        if (Input.GetKeyDown(KeyCode.G) && !_isCharging)
        {
            if (Time.time < _nextThrowTime) return;

            if(CoinCount <= 0)
            {
                ShowNotice("던질 동전이 없습니다.");
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
        //G키를 누르고 있는 동안 투척 힘 충전
        if (Input.GetKey(KeyCode.G))
        {
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, _fullChargeTime);
            UpdateChargeGauge();
        }
        //G키를 놓으면 충전량을 저장하고 투척 애니메이션 시작
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
    //충전 상태를 초기화하고 게이지 숨기기
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
    //다른 창으로 전환하면 진행 중인 충전과 투척 취소
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
            ShowNotice("던질 동전이 없습니다.");
            return;
        }

        Vector3 origin = _throwOrigin.position;
        //안전 기준점과 생성 위치 사이에 장애물이 있으면 투척 차단
        if (_throwSatfetyOrigin != null && Physics.Linecast(_throwSatfetyOrigin.position, origin, _environmentMask, QueryTriggerInteraction.Ignore))
        {
            ShowNotice("장애물에 너무 가까워 던질 수 없습니다.");
            return;
        }
        //동전이 장애물 안에서 생성되는 것 방지
        if (Physics.CheckSphere(origin, _spawnCheckRadius, _environmentMask, QueryTriggerInteraction.Ignore))
        {
            ShowNotice("장애물에 너무 가까워 던질 수 없습니다.");
            return;
        }
        //화면 중앙에서 최대 15m 앞의 조준 위치 계산
        Ray aimRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = aimRay.GetPoint(15f);

        if(Physics.Raycast(aimRay,out RaycastHit hit, 15f, _environmentMask, QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
        }

        Vector3 direction = (aimPoint - origin).normalized;
        //충전량에 따라 속도를 정하고 위쪽 힘 추가
        float throwSpeed = Mathf.Lerp(_minThrowSpeed, _maxThrowSpeed, Mathf.Clamp01(chargeRatio));

        Vector3 launchVelocity = direction * throwSpeed + Vector3.up * _upwardSpeed;
        //동전을 생성하고 초기 속도와 플레이어 콜라이더 전달
        ThrowableCoin coin = Instantiate(_coinPrefab, origin, Quaternion.identity);
        coin.Initialize(this,launchVelocity,_playerColliders);

        CoinCount--;
        _nextThrowTime = Time.time + _throwCooldown;

        PrintCount();
    }
    //최대 개수를 넘지 않을 때만 동전 회수
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
        //충전량을 저장하고 애니메이션의 동전 발사 신호 대기
        _pendingPower = power;
        _releasePending = true;
        _isThrowing = true;

        _throwTimeoutAt = Time.time + _throwAnimationTimeout;

        _animator.ResetTrigger(ThrowCoinHash);
        _animator.SetTrigger(ThrowCoinHash);
    }
    //애니메이션에서 동전이 손을 떠나는 시점에 호출
    public void ReleaseCoin()
    {
        if(!isActiveAndEnabled || !_isThrowing || !_releasePending) return;
        //같은 투척에서 동전이 여러 번 생성되지 않도록 처리
        _releasePending = false;

        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked || (_playerCover != null && _playerCover.IsInCover))
            return;
        TryThrow(_pendingPower);
    }
    //투척 애니메이션이 끝나면 다음 투척 허용
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
        //투척 중이었다면 해당 애니메이션을 빈 상태로 전환
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
