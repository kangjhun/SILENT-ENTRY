using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        bool suspicious = state == EnemyFSM.EnemyState.Suspicious;
        bool chasing = state == EnemyFSM.EnemyState.Chase;
        bool invesigating = state == EnemyFSM.EnemyState.Investigate || state == EnemyFSM.EnemyState.Search;
        bool show = suspicious || chasing || invesigating;

        _statusCanvas.enabled = show;

        if (!show) return;
        _statusCanvas.transform.rotation = _playerCamera.transform.rotation;
        _stateText.text = chasing ? "!" : "?";
        _stateText.color = chasing ? _chaseColor : _suspiciousColor;

        _detectionSlider.gameObject.SetActive(suspicious);

        if (suspicious)
        {
            _detectionSlider.SetValueWithoutNotify(Mathf.Clamp01(_fsm.DetectionAmount));
        }
    }

    void OnDisable()
    {
        if(_statusCanvas != null )
            _statusCanvas.enabled = false;
    }
}
