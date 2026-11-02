using UnityEngine;
using UnityEngine.UI;
//스태미나 잔량과 탈진 상태를 게이지로 표시
public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] PlayerMovement _movement;
    [SerializeField] Slider _slider;
    [SerializeField] Image _fillImage;

    [SerializeField] Color _normalColor = new Color(0.8f, 0.9f, 0.85f);
    [SerializeField] Color _exhaustedColor = new Color(1f, 0.35f, 0.2f);
     void Awake()
    {
        if(_movement == null || _slider == null)
        {
            Debug.LogError("PlayerMovement와 Slider를 연결하세요.", this);
            enabled = false;
            return;
        }

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.interactable = false;
    }

     void Start()
    {
        RefreshUI();
    }

    void LateUpdate()
    {
        RefreshUI();
    }

    void RefreshUI()
    {
        float ratio = _movement.StaminaRatio;
        //달리거나 탈진 중이거나 스태미나가 덜 찼으면 표시
        bool show = _movement.IsRunning || _movement.IsExhausted || ratio < 0.999f;

        if(_slider.gameObject.activeSelf != show)
        {
            _slider.gameObject.SetActive(show);
        }
        //현재 스태미나 비율을 게이지에 반영
        _slider.SetValueWithoutNotify(ratio);
        //탈진 상태에 따라 게이지 색상 변경
        if(_fillImage != null)
        {
            _fillImage.color = _movement.IsExhausted ? _exhaustedColor : _normalColor;
        }
    }
}
