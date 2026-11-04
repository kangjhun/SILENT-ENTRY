using UnityEngine;
using UnityEngine.UI;
using TMPro;
//적 AI가 갱신된 뒤 UI 처리
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(EnemyFSM))]
public class EnemyStatusUI : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] EnemyFSM _fsm;
    [SerializeField] Canvas _statusCanvas;
    [SerializeField] TMP_Text _stateText;
    [SerializeField] Slider _detectionSlider;
    [SerializeField] Camera _playerCamera;

    [Header("색상")]
    [SerializeField] Color _suspiciousColor = new Color(1f, 0.8f, 0f);
    [SerializeField] Color _chaseColor = new Color(1f, 0.15f, 0.1f);

     void Awake()
    {
        if( _fsm == null )  
            _fsm = GetComponent<EnemyFSM>();

        if (_playerCamera == null)
            _playerCamera = Camera.main;
        //필수 연결이 없으면 UI를 숨기고 실행 중단
        if(_statusCanvas == null || _stateText == null || _detectionSlider == null || _playerCamera == null)
        {
            Debug.LogError("EnemyStateUI의 Canvas, Text, Slider, Camera를 연결하세요.", this);
            if(_statusCanvas != null)
            {
                _statusCanvas.enabled = false;
            }
            enabled = false;
            return;
        }
        //시작할 때 감지 게이지를 비우고 UI 숨기기
        _detectionSlider.SetValueWithoutNotify(0f);
        _statusCanvas.enabled = false;
    }

    void LateUpdate()
    {
        if (!_fsm.isActiveAndEnabled)
        {
            _statusCanvas.enabled = false;
            return;
        }
        EnemyFSM.EnemyState state = _fsm.CurrentState;
        //감지, 추격, 조사, 수색 중일 때만 UI 표시
        bool suspicious = state == EnemyFSM.EnemyState.Suspicious;
        bool chasing = state == EnemyFSM.EnemyState.Chase;
        bool invesigating = state == EnemyFSM.EnemyState.Investigate;
        bool searching =  state == EnemyFSM.EnemyState.Search;
        bool show = suspicious || chasing || invesigating || searching;

        _statusCanvas.enabled = show;

        if (!show) return;
        //UI가 플레이어 카메라를 향하도록 회전
        _statusCanvas.transform.rotation = _playerCamera.transform.rotation;

        if (chasing)
        {
            _stateText.text = "!";
        }else if (suspicious)
        {
            _stateText.text = "?";
        }else if (invesigating)
        {
            _stateText.text = "소음 확인 중";
        }else if (searching)
        {
            _stateText.text = "조사 중";
        }
        _stateText.color = chasing ? _chaseColor : _suspiciousColor;
        //플레이어를 감지하는 동안만 게이지 표시
         _detectionSlider.gameObject.SetActive(suspicious);

        if (suspicious)
        {
            _detectionSlider.SetValueWithoutNotify(Mathf.Clamp01(_fsm.DetectionAmount));
        }
    }

    void OnDisable()
    {//비활성화되면 UI도 숨기기
        if(_statusCanvas != null )
            _statusCanvas.enabled = false;
    }
}
