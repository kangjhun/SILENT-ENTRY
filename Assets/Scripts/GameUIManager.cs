using TMPro;
using Unity.VisualScripting;
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

    [Header("목표 위치 표시")]
    [SerializeField] Camera _markerCamera;
    [SerializeField] RectTransform _markerCanvasRect;
    [SerializeField] TMP_Text _objectiveMarkerText;

    [SerializeField] Transform _objectiveMarkerPoint;
    [SerializeField] Transform _escapeMarkerPoint;

    [SerializeField] Vector2 _markerScreenMargin = new Vector2(110f, 40f);

    [Header("일시적인 안내 메시지")]
    [SerializeField] GameObject _noticePanel;
    [SerializeField] TMP_Text _noticeText;
    [SerializeField, Min(0.1f)] float _noticeDuration = 2f;

    [Header("시작 안내")]
    [SerializeField] bool _showStartNotice = true;
    [SerializeField, Min(0.1f)] float _startNoticeDuration = 5f;

    [Header("일시정지 메뉴")]
    [SerializeField] GameObject _pausePanel;
    [SerializeField] Behaviour _cameraInputController;

    [Header("조작법 화면")]
    [SerializeField] GameObject _pauseMenuBox;
    [SerializeField] GameObject _controlsBox;

    [Header("시작 화면")]
    [SerializeField] string _mainMenuSceneName = "MainMenu";

    [Header("게임 결과")]
    [SerializeField] GameObject _resultPanel;
    [SerializeField] TMP_Text _resultTitle;
    [SerializeField] TMP_Text _resultMessage;

    bool _resultShown;
    bool _isPaused;
    bool _cameraInputWasEnable;
    bool _isReturningToMenu;

    float _timeScaleBeforePause = 1f;
    float _noticeTimer;
    void Awake()
    {
        PrepareText(_interactionText);
        PrepareText(_objectiveText);
        PrepareText(_coinText);
        PrepareText(_objectiveMarkerText);
        UpdateBackgrounds();

        _isPaused = false;
        if(_pausePanel != null)
            _pausePanel.SetActive(false);

        _resultShown = false;
        if(_resultPanel != null)
            _resultPanel.SetActive(false);
        HideNotice();
        ShowPauseMenu();
    }

     void Start()
    {
        if (!_showStartNotice) return;
        ShowNotice("목표물을 확보하고 현관으로 탈출하세요.\n" + "조작법은 ESC 메뉴에서 확인할 수 있습니다.");
        if (_noticeTimer > 0f)
            _noticeTimer = _startNoticeDuration;
    }

    void Update()
    {
        if (_gameManagr == null || !_gameManagr.IsPlaying) return;

        if(!Input.GetKeyDown(KeyCode.Escape)) return;

        if (!_isPaused)
            PauseGame();
        else if (_controlsBox != null && _controlsBox.activeSelf)
            CloseControls();
        else
            ResumeGame();
    }

    void ShowPauseMenu()
    {
        if(_pauseMenuBox != null)
            _pauseMenuBox.SetActive(true);
        if(_controlsBox != null)    
            _controlsBox.SetActive(false);
    }

    public void OpenControls()
    {
        if(!_isPaused || _controlsBox == null) return;
        if(_pauseMenuBox != null)
            _pauseMenuBox.SetActive(false);
        _controlsBox.SetActive(true);
    }

    public void CloseControls()
    {
        if (!_isPaused) return;
        ShowPauseMenu();
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
        ShowPauseMenu() ;   
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
        UpdateResultUI();
        bool isPlaying = _gameManagr != null && _gameManagr.IsPlaying && !_isPaused;

        UpdateInteractionUI(isPlaying);
        UpdateObjectiveUI(isPlaying);
        UpdateCoinUI(isPlaying);
        UpdateObjectiveMarkerUI(isPlaying);
        UpdateBackgrounds();
        UpdateNoticeUI();
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

    void UpdateObjectiveMarkerUI(bool isPlaying)
    {
        if (_objectiveMarkerText == null) return;
        if(!isPlaying || _gameManagr == null || _markerCamera == null || _markerCanvasRect == null)
        {
            SetText(_objectiveMarkerText,string.Empty);
            return;
        }

        bool hasObjective = _gameManagr.HasObjective;
        Transform targetPoint = hasObjective ? _escapeMarkerPoint : _objectiveMarkerPoint;
        if(targetPoint == null || !targetPoint.gameObject.activeInHierarchy)
        {
            SetText(_objectiveMarkerText, string.Empty);
            return;
        }

        Vector3 screenPoint = _markerCamera.WorldToScreenPoint(targetPoint.position);
        if(screenPoint.z < _markerCamera.nearClipPlane || screenPoint.z > _markerCamera.farClipPlane)
        {
            SetText(_objectiveMarkerText, string.Empty);
            return;
        }

        Canvas canvas = _markerCanvasRect.GetComponent<Canvas>();
        float canvasScale = canvas != null ? canvas.scaleFactor : 1f;

        float marginX = _markerScreenMargin.x * canvasScale;
        float marginY = _markerScreenMargin.y * canvasScale;

        Rect cameraRect = _markerCamera.pixelRect;

        bool outsideScreen = screenPoint.x < cameraRect.xMin + marginX || screenPoint.x > cameraRect.xMax - marginX || screenPoint.y < cameraRect.yMin + marginY || screenPoint.y > cameraRect.yMax - marginY;
        if (outsideScreen)
        {
            SetText(_objectiveMarkerText,string.Empty);
            return;
        }

        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(_markerCanvasRect, new Vector2(screenPoint.x,screenPoint.y),null,out Vector2 localPoint))
        {
            SetText(_objectiveMarkerText, string.Empty);
            return;
        }

        _objectiveMarkerText.rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        string message = hasObjective ? "◆ 탈출 지점" : "◆ 목표물";
        SetText(_objectiveMarkerText,message);  
    }

    void UpdateResultUI()
    {
        if(_gameManagr == null || _gameManagr.IsPlaying || _resultShown || _resultPanel == null) return;
        _resultShown = true;
        _isPaused = false;
        if(_pausePanel != null)
            _pausePanel.SetActive(false);
        bool success = _gameManagr.CurrentState == GameManager.GameState.Success;

        if(_resultTitle != null)
        {
            _resultTitle.text = success ? "임무 완료" : "임무 실패";
            _resultTitle.color = success ? new Color32(199, 170, 112, 255) : new Color32(220, 110, 110, 255);
        }

        if(_resultMessage != null)
            _resultMessage.text = success ? "목표물을 확보하고 탈출했습니다." : "경비에게 붙잡혔습니다.";

        if(_cameraInputController != null)
            _cameraInputController.enabled = false;

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        _resultPanel.SetActive(true);
        _resultPanel.transform.SetAsLastSibling();
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

    public void ShowNotice(string message)
    {
        if (_gameManagr == null || !_gameManagr.IsPlaying || _isPaused || _noticePanel == null || _noticeText == null || string.IsNullOrWhiteSpace(message))
            return;
        if (_noticeTimer > 0f && _noticeText.text == message)
            return;
        _noticeText.text = message;
        _noticeTimer = _noticeDuration;
        _noticePanel.SetActive(true);   
    }

    void UpdateNoticeUI()
    {
        if(_gameManagr == null || !_gameManagr.IsPlaying)
        {
            HideNotice();
            return;
        }

        if (_isPaused)
        {
            if(_noticePanel != null)
                _noticePanel.SetActive(false);

            return;
        }

        if (_noticeTimer <= 0f)
        {
            HideNotice();
            return;
        }
        _noticeTimer = Mathf.Max(0f,_noticeTimer - Time.deltaTime);
        if(_noticeTimer <=0f)
        {
            HideNotice();
            return;
        }
        if(_noticePanel != null && !_noticePanel.activeSelf)
            _noticePanel.SetActive(true) ;
    }

    void HideNotice()
    {
        _noticeTimer = 0f;
        if(_noticeText != null)
            _noticeText.text = string.Empty;
        if(_noticePanel != null)
            _noticePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        if(_isReturningToMenu) return;
        if(string.IsNullOrWhiteSpace(_mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(_mainMenuSceneName))
        {
            Debug.LogError("시작 화면 씬 이름과 빌드 씬 목록을 확인하세요.", this);
            return;
        }

        _isReturningToMenu = true;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
    void OnDisable()
    {
        SetText(_interactionText,string.Empty);
        SetText(_objectiveText,string.Empty);   
        SetText(_coinText,string.Empty);    
        SetText(_objectiveMarkerText,string.Empty);

        UpdateBackgrounds();
        HideNotice() ;
    }
}
