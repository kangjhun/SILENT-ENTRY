using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//게임 안내, 일시정지 메뉴, 결과 화면 관리
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
    bool _coinUIVisible;

    int _lastCoinCount = -1;
    int _lastMaxCoins = -1;

    float _timeScaleBeforePause = 1f;
    float _noticeTimer;
    void Awake()
    {
        //텍스트와 각 화면의 초기 표시 상태 설정
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
        //게임 시작 시 목표와 조작법 안내
        if (!_showStartNotice) return;
        ShowNotice("목표물을 확보하고 현관으로 탈출하세요.\n" + "조작법은 ESC 메뉴에서 확인할 수 있습니다.");
        if (_noticeTimer > 0f)
            _noticeTimer = _startNoticeDuration;
    }

    void Update()
    {
        if (_gameManagr == null || !_gameManagr.IsPlaying) return;

        if(!Input.GetKeyDown(KeyCode.Escape)) return;
        //ESC로 일시정지, 조작법 닫기, 게임 재개
        if (!_isPaused)
            PauseGame();
        else if (_controlsBox != null && _controlsBox.activeSelf)
            CloseControls();
        else
            ResumeGame();
    }
    //조작법 화면을 닫고 기본 일시정지 메뉴 표시
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
        //기존 게임 속도를 저장하고 시간 정지
        _isPaused = true;
        _timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        //메뉴 사용 중 카메라 조작 중단
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
        //일시정지 전 게임 속도와 카메라 조작 상태 복구
        _isPaused = false;
        _pausePanel.SetActive(false);
        Time.timeScale = _timeScaleBeforePause;
        if (_cameraInputController != null)
            _cameraInputController.enabled = _cameraInputWasEnable;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 
    }
    //현재 씬을 다시 불러와 게임 재시작
    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        //에딭터에서는 플레이 모드 종료
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }

    void LateUpdate()
    {
        UpdateResultUI();
        bool isPlaying = _gameManagr != null && _gameManagr.IsPlaying && !_isPaused;
        //게임 상태에 맞춰 화면 정보 갱신
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
        //텍스트가 마우스 클릭을 가로막지 않도록 설정
        text.raycastTarget = false;
        text.enabled = false;
    }
    //현재 상호작용할 수 있는 대상의 안내 표시
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
        //목표물 획득 여부에 따라 현재 목표 변경
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
        //목표물을 얻으면 표시 대상을 탈출 지점으로 변경
        bool hasObjective = _gameManagr.HasObjective;
        Transform targetPoint = hasObjective ? _escapeMarkerPoint : _objectiveMarkerPoint;
        if(targetPoint == null || !targetPoint.gameObject.activeInHierarchy)
        {
            SetText(_objectiveMarkerText, string.Empty);
            return;
        }
        //월드 위치를 화면 위치로 변환하고 카메라 앞뒤 범위 확인
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
        //화면 가장자리 여백을 벗어나면 표시 숨기기
        bool outsideScreen = screenPoint.x < cameraRect.xMin + marginX || screenPoint.x > cameraRect.xMax - marginX || screenPoint.y < cameraRect.yMin + marginY || screenPoint.y > cameraRect.yMax - marginY;
        if (outsideScreen)
        {
            SetText(_objectiveMarkerText,string.Empty);
            return;
        }
        //화면 위치를 캔버스 안의 위치로 변환
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
        //게임 종료 후 결과 화면을 한 번만 표시
        if(_gameManagr == null || _gameManagr.IsPlaying || _resultShown || _resultPanel == null) return;
        _resultShown = true;
        _isPaused = false;
        if(_pausePanel != null)
            _pausePanel.SetActive(false);
        bool success = _gameManagr.CurrentState == GameManager.GameState.Success;
        //성공 여부에 따라 제목, 색상, 설명 변경
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
        //결과 화면이 다른 UI보다 앞에 표시되도록 설정
        _resultPanel.SetActive(true);
        _resultPanel.transform.SetAsLastSibling();
    }
    //남은 동전 수와 최대 개수 표시
    void UpdateCoinUI(bool isPlaying)
    {
        bool show = isPlaying && _coinThrower != null;

        if (!show)
        {
            SetText(_coinText, string.Empty);
            _coinUIVisible = false;
            return;
        }

        int count = _coinThrower.CoinCount;
        int max = _coinThrower.MaxCoins;
        //표시 상태나 개수가 바뀌었을 때만 문자열 생성
        if (_coinUIVisible && count == _lastCoinCount && max == _lastMaxCoins) return;
        
        _coinUIVisible = true;
        _lastCoinCount = count;
        _lastMaxCoins = max;

        SetText(_coinText,$"<color=#C7AA70>동전</color> {count}/{max}");
    }
    //내용이 비어 있으면 숨기고, 달라진 내용만 갱신
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
    //텍스트 표시 상태에 맞춰 배경도 표시하거나 숨기기
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
        //같은 안내가 표시 중이면 표시 시간을 다시 시작하지 않음
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
        //일시정지 중에는 남은 안내 시간을 유지하고 숨기기
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
        //안내 시간이 끝나면 메시지 숨기기
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
        //시작 화면 씬을 불러올 수 있는지 확인
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
        //비활성화되면 게임 안내와 배경 숨기기
        SetText(_interactionText,string.Empty);
        SetText(_objectiveText,string.Empty);   
        SetText(_coinText,string.Empty);    
        SetText(_objectiveMarkerText,string.Empty);

        UpdateBackgrounds();
        HideNotice() ;
        _coinUIVisible = false;
    }
}
