using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameUIManager : MonoBehaviour
{
    [Header("게임 정보")]
    [SerializeField] GameManager _gameManagr;
    [SerializeField] PlayerInteraction _playerInteraction;
    [SerializeField] PlayerCoinThrower _coinThrower;

    [Header("UI 텍스트")]
    [SerializeField] TMP_Text _interactionText;
    [SerializeField] TMP_Text _objectiveText;
    [SerializeField] TMP_Text _coinText;

    [Header("UI 배경")]
    [SerializeField] Image _interactionBackground;
    [SerializeField] Image _objectiveBackground;
    [SerializeField] Image _coinBackground;
    [SerializeField] Image _objectiveAccent;

    [Header("일시정지 메뉴")]
    [SerializeField] GameObject _pausePanel;
    [SerializeField] Behaviour _cameraInputController;

    bool _isPaused;
    bool _cameraInputWasEnable;
    float _timeScaleBeforePause = 1f;
    void Awake()
    {
        PrepareText(_interactionText);
        PrepareText(_objectiveText);
        PrepareText(_coinText);
        UpdateBackgrounds();

        _isPaused = false;
        if(_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    void Update()
    {
        if (_gameManagr == null || !_gameManagr.IsPlaying) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }
    public void PauseGame()
    {
        if (_isPaused || _pausePanel == null || _gameManagr == null || !_gameManagr.IsPlaying) return;
        _isPaused = true;
        _timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        if(_cameraInputController != null)
        {
            _cameraInputWasEnable = _cameraInputController.enabled;
            _cameraInputController.enabled = false; 
        }

        _pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ResumeGame()
    {
        if (!_isPaused || _gameManagr == null || !_gameManagr.IsPlaying) return;
        _isPaused = false;
        _pausePanel.SetActive(false);
        Time.timeScale = _timeScaleBeforePause;
        if (_cameraInputController != null)
            _cameraInputController.enabled = _cameraInputWasEnable;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }

    void LateUpdate()
    {
        bool isPlaying = _gameManagr != null && _gameManagr.IsPlaying && !_isPaused;

        UpdateInteractionUI(isPlaying);
        UpdateObjectiveUI(isPlaying);
        UpdateCoinUI(isPlaying);
        UpdateBackgrounds();
    }

    void PrepareText(TMP_Text text)
    {
        if(text == null) return;
        text.raycastTarget = false;
        text.enabled = false;
    }

    void UpdateInteractionUI(bool isPlaying)
    {
        string message = isPlaying && _playerInteraction != null ? _playerInteraction.GetInteractionPrompt() : string.Empty;
        SetText(_interactionText,message);
    }
    
    void UpdateObjectiveUI(bool isPlaying)
    {
        if (!isPlaying)
        {
            SetText(_objectiveText,string.Empty);
            return;
        }

        string message = _gameManagr.HasObjective ? "<size=22><color=#D5DBE3>현재 목표</color></size>\n현관으로 이동해 탈출하세요." : "<size=22><color=#D5DBE3>현재 목표</color></size>\n목표물을 확보하세요.";
        SetText(_objectiveText,message);    
    }

    void UpdateCoinUI(bool isPlaying)
    { 
        string message = isPlaying && _coinThrower != null ? $"<color=#C7AA70>동전</color>  {_coinThrower.CoinCount}/{_coinThrower.MaxCoins}" : string.Empty;
        SetText(_coinText,message);
    }

    void SetText(TMP_Text text, string message)
    {
        if(text == null) { return; }
        bool show = !string.IsNullOrEmpty(message);

        if(text.enabled != show)
            text.enabled=show;
        if(text.text != message)
            text.text=message;
    }

    void UpdateBackgrounds()
    {
        SetBackground(_interactionBackground, _interactionText);
        SetBackground(_objectiveBackground, _objectiveText);
        SetBackground(_coinBackground, _coinText);
        SetBackground(_objectiveAccent, _objectiveText); 
    }

    void SetBackground(Image background, TMP_Text text)
    {
        if(background == null) { return; }
        bool show = text != null && text.isActiveAndEnabled;
        if(background.enabled != show) background.enabled=show;
    }

    void OnDisable()
    {
        SetText(_interactionText,string.Empty);
        SetText(_objectiveText,string.Empty);   
        SetText(_coinText,string.Empty);    
        UpdateBackgrounds();
    }
}
