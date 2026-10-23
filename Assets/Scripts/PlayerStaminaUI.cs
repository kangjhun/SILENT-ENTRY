using UnityEngine;
using UnityEngine.UI;
public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] PlayerMovement _movement;
    [SerializeField] Slider _slider;
    [SerializeField] Image _fillImage;

    [SerializeField] Color _normalColor = new Color(0.8f, 0.9f, 0.85f);
    [SerializeField] Color _exhaustedColor = new Color(1f, 0.35f, 0.2f);
     void Awake()
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

        bool show = _movement.IsRunning || _movement.IsExhausted || ratio < 0.999f;

        if(_slider.gameObject.activeSelf != show)
        {
            _slider.gameObject.SetActive(show);
        }

        _slider.SetValueWithoutNotify(ratio);

        if(_fillImage != null)
        {
            _fillImage.color = _movement.IsExhausted ? _exhaustedColor : _normalColor;
        }
    }
}
