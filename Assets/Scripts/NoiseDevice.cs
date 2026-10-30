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

    [Header("라디오 사운드")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField, Min(0.1f)] float _playDuration = 8f;
    [SerializeField, Min(0.1f)] float _noiseInterval = 0.5f;
    


    //다음 작동이 가능한 게임 시간
    float _nextUseTime;
    float _soundEndTime;
    float _nextNoiseTIme;

    bool _soundActive;

    //상호작용 안내에 표시할 이름
    public string DisplayName => _soundActive ? $"{_displayName} 재생 중" : $"{_displayName} 켜기";

    //거리, 방향, 가림 판정에 사용할 월드 좌표
    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    //쿨타임 중에도 상호작용 대상으로 선택되도록 유지
    public bool CanInteract => isActiveAndEnabled;
    public void Interact()
    {
        if (!CanInteract) return;
        if (_soundActive || Time.time < _nextUseTime) return;
        if(_audioSource == null || _audioSource.clip == null)
        {
            Debug.Log("라디오의 Audio Source와 음악을 연결하세요.", this);
            return;
        }

        //다음 작동 가능 시간 갱신
        _nextUseTime = Time.time + _cooldown;

        _audioSource.loop = true;
        _audioSource.Play();

        _soundActive = true;
        _soundEndTime = Time.time + _playDuration;
        _nextNoiseTIme = Time.time + _noiseInterval;

        EmitNoise();

    }

    void EmitNoise()
    {
        if(_enemies == null) return;    
        foreach(EnemyFSM enemy in _enemies)
        {
            if (enemy == null) continue;
            enemy.HearNoise(transform.position, _hearingRadius,1);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || !_soundActive) return;
        if (Time.time >= _soundEndTime)
        {
            if (_audioSource != null)
                _audioSource.Stop();
            _soundActive = false;
            return;
        }

        if (Time.time < _nextNoiseTIme) return;
        _nextNoiseTIme = Time.time + _noiseInterval;
        EmitNoise();
    }

    void OnDisable()
    {
        if(_audioSource != null) 
            _audioSource.Stop();
        _soundActive = false;
    }
}
