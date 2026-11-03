
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

    [Header("투척 예상 경로")]
    [SerializeField] LineRenderer _trajectoryLine;
    [SerializeField] Transform _impactMarker;
    //예측에 사용하는 동전의 대략적인 반경
    [SerializeField, Min(0.001f)] float _predictionRadius = 0.025f;

    [SerializeField, Min(0.5f)] float _predictionDuration = 3f;
    [SerializeField, Min(0.02f)] float _previewInterval = 0.05f;

    bool _isCharging;
    bool _isThrowing;
    bool _releasePending;//애니메이션에서 동전을 놓을 시점을 기다리는 중

    float _pendingPower;
    float _throwTimeoutAt;
    float _chargeTime;
    float _nextPreviewTime;

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
        if(_trajectoryLine != null)
        {
            _trajectoryLine.useWorldSpace = true;
            _trajectoryLine.loop = false;

            _trajectoryLine.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            _trajectoryLine.widthMultiplier = 0.025f;

            _trajectoryLine.positionCount = 0;
            _trajectoryLine.enabled = false;
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
        _nextPreviewTime = 0f;

        if(_chargeSlider !=  null)
            _chargeSlider.value = 0f;

        if(_chargeGaugeRoot != null)
            _chargeGaugeRoot.SetActive(false);

        HideTrajectory();
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

        if (IsThrowBlocked(origin))
        {
            ShowNotice("장애물에 너무 가까워 던질 수 없습니다.");
            return;
        }

        Vector3 launchVelocity = CalculateLaunchVelocity(origin, chargeRatio);
        ThrowableCoin coin = Instantiate(_coinPrefab, origin, Quaternion.identity);

        coin.Initialize(this, launchVelocity,_playerColliders);

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

     void LateUpdate()
    {
        if(!_isCharging || Time.timeScale <=0f || Cursor.lockState != CursorLockMode.Locked || (_playerCover != null && _playerCover.IsInCover))
        {
            HideTrajectory();
            return;
        }

        if (Time.time < _nextPreviewTime) return;
        _nextPreviewTime = Time.time + _previewInterval;
        DrawTrajectory();
    }

    Vector3 CalculateLaunchVelocity(Vector3 origin, float chargeRatio)
    {
        Ray aimRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = aimRay.GetPoint(15f);

        if(Physics.Raycast(aimRay,out RaycastHit hit, 15f, _environmentMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;
        Vector3 direction = (aimPoint - origin).normalized;
        float speed = Mathf.Lerp(_minThrowSpeed, _maxThrowSpeed, Mathf.Clamp01(chargeRatio));

        return direction * speed + Vector3.up * _upwardSpeed;
    }

    bool IsThrowBlocked(Vector3 origin)
    {
        if (_throwSatfetyOrigin != null && Physics.Linecast(_throwSatfetyOrigin.position, origin, _environmentMask, QueryTriggerInteraction.Ignore)) return true;
        return Physics.CheckSphere(origin, Mathf.Max(_spawnCheckRadius, _predictionRadius), _environmentMask, QueryTriggerInteraction.Ignore);
    }

    void DrawTrajectory()
    {
        if(_trajectoryLine == null || _playerCamera == null || _throwOrigin == null || _coinPrefab == null)
        {
            HideTrajectory();
            return;
        }
        Vector3 position = _throwOrigin.position;
        //던질 공간이 없으면 예측 표시도 숨기기
        if (IsThrowBlocked(position))
        {
            HideTrajectory();
            return;
        }

        Vector3 velocity = CalculateLaunchVelocity(position, ChargeRatio);

        float stepTime = Mathf.Max(Time.fixedDeltaTime, 0.001f);
        int steps = Mathf.Clamp(Mathf.CeilToInt(_predictionDuration / stepTime), 1, 500);

        _trajectoryLine.enabled = true;
        _trajectoryLine.positionCount = steps + 1;
        _trajectoryLine.SetPosition(0,position);

        if(_impactMarker != null)
            _impactMarker.gameObject.SetActive(false);
        for(int i = 1; i <= steps; i++)
        {
            velocity += Physics.gravity * stepTime;
            Vector3 nextPosition = position + velocity * stepTime;

            Vector3 segment = nextPosition - position;
            float distance = segment.magnitude;

            if(distance > 0.0001f && Physics.SphereCast(position,_predictionRadius,segment/distance,out RaycastHit hit, distance, _environmentMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 endPoint = position + segment / distance * hit.distance;
                _trajectoryLine.SetPosition(i,endPoint);
                _trajectoryLine.positionCount = i +1;   

                if(_impactMarker != null)
                {
                    _impactMarker.position = hit.point + hit.normal * 0.02f;
                    _impactMarker.gameObject.SetActive(true);
                }

                return;
            }
            _trajectoryLine.SetPosition(i, nextPosition);
            position = nextPosition;
        }
    }

    void HideTrajectory()
    {
        if(_trajectoryLine != null)
            _trajectoryLine.enabled = false;
        if (_impactMarker != null)
            _impactMarker.gameObject.SetActive(false);
    }
}
