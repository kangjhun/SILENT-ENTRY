using UnityEngine;
using Unity.Cinemachine;
[RequireComponent(typeof(CinemachineBasicMultiChannelPerlin))]
public class PlayerRunCameraShake : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] PlayerMovement _movement;
    [SerializeField] PlayerCover _cover;
    [SerializeField] PlayerCoinThrower _coinThrower;

    [Header("달리기 흔들림")]
    [SerializeField, Min(0f)] float _runAmplitude = 0.15f;
    [SerializeField, Min(0f)] float _runFrequency = 1.5f;

    [Tooltip("흔들림이 켜지고 꺼지는 전환 시간")]
    [SerializeField, Min(0.01f)] float _transitionTime = 0.2f;

    CinemachineBasicMultiChannelPerlin _noise;

     void Awake()
    {
        _noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        _noise.AmplitudeGain = 0f;
        if(_movement == null)
        {
            Debug.LogError("PlayerRunCameraShake에 PlayerMovement를 연결하세요.");
            enabled = false;
        }
    }

     void LateUpdate()
    {
        bool inCover = _cover != null && _cover.IsInCover;

        bool usingCoin = _coinThrower != null && (_coinThrower.IsCharging || _coinThrower.IsThrowing);
        bool canShake = _movement.isActiveAndEnabled && _movement.IsRunning && !inCover && !usingCoin && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f;
        //일시정지, 메뉴 중에는 즉시 제거
        if(Time.timeScale <=0f || Cursor.lockState != CursorLockMode.Locked)
        {
            _noise.AmplitudeGain = 0f;
            return;
        }
        float targetAmplitude = canShake ? _runAmplitude : 0f;
        //시작과 정지 시 흔들림을 부드럽게 변경
        float blend = 1f - Mathf.Exp(-3f * Time.deltaTime / _transitionTime);

        _noise.AmplitudeGain = Mathf.Lerp(_noise.AmplitudeGain,targetAmplitude, blend);
        if (targetAmplitude == 0f && _noise.AmplitudeGain < 0.001f)
            _noise.AmplitudeGain = 0f;
        _noise.FrequencyGain = _runFrequency;
    }

     void OnDisable()
    {
        if (_noise != null)
            _noise.AmplitudeGain = 0f;
    }
}
