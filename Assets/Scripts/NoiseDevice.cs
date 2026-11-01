using UnityEngine;
//라디오를 켜서 주변 적을 소음 위치로 유도
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
    


    
    float _nextUseTime;//다시 켤 수 있는 시간
    float _soundEndTime;//재생이 끝나는 시간
    float _nextNoiseTIme;//적에게 소음을 다시 알릴 시간

    bool _soundActive;

    //재생상태에 따라 상호작용 안내 변경
    public string DisplayName => _soundActive ? $"{_displayName} 재생 중" : $"{_displayName} 켜기";


    public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

    //재생 중이나 쿨타임 중에도 안내를 표시할 수 있도록 유지
    public bool CanInteract => isActiveAndEnabled;
    public void Interact()
    {
        if (!CanInteract) return;
        //재생 중이거나 쿨타임이 남아 있으면 작동하지 않음
        if (_soundActive || Time.time < _nextUseTime) return;
        if(_audioSource == null || _audioSource.clip == null)
        {
            Debug.Log("라디오의 Audio Source와 음악을 연결하세요.", this);
            return;
        }

        
        _nextUseTime = Time.time + _cooldown;
        //정해진 재생 시간 동안 음악 반복
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
        //연결된 적에게 소음 위치, 들리는 범위, 우선순위 전달
        foreach(EnemyFSM enemy in _enemies)
        {
            if (enemy == null) continue;
            enemy.HearNoise(transform.position, _hearingRadius,1);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || !_soundActive) return;
        //재생 시간이 끝나면 라디오 끄기
        if (Time.time >= _soundEndTime)
        {
            if (_audioSource != null)
                _audioSource.Stop();
            _soundActive = false;
            return;
        }
        //재생 중에는 일정 간격으로 적에게 소음을 알림
        if (Time.time < _nextNoiseTIme) return;
        _nextNoiseTIme = Time.time + _noiseInterval;
        EmitNoise();
    }

    void OnDisable()
    {
        //비활성화되면 음악과 작동 상태 정리
        if(_audioSource != null) 
            _audioSource.Stop();
        _soundActive = false;
    }
}
