using UnityEngine;
using UnityEngine.SceneManagement;
//시작 화면의 게임 시작, 조작법, 종료 버튼 처리
public class MainMenuUI : MonoBehaviour
{
    [Header("시작할 게임 씬")]
    [SerializeField] string _gameSceneName;

    [Header("화면")]
    [SerializeField] GameObject _mainMenuBox;
    [SerializeField] GameObject _controlsBox;

    bool _isLoading;

     void Awake()
    {
        //게임 시간을 복구하고 메뉴에서 사용할 커서 표시
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CloseControls();
    }
    void Update()
    {
        if (_isLoading) return;
        //조작법 화면에서 ESC를 누르면 기본 메뉴로 돌아감
        if (Input.GetKeyDown(KeyCode.Escape) && _controlsBox != null && _controlsBox.activeSelf)
        {
            CloseControls();
        }
    }

    public void StartGame()
    {
        //씬을 중복으로 불러오지 않도록 차단
        if(_isLoading) return;
        if (string.IsNullOrWhiteSpace(_gameSceneName))
        {
            Debug.LogError("시작할 게임 씬 이름을 입력하세요.", this);
            return;
        }
        //설정된 씬을 불러올 수 있는지 확인
        if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
        {
            Debug.LogError($"'{_gameSceneName}' 씬을 불러올 수 없습니다. " + "씬 이름과 빌드 씬 목록을 확인하세요.",this);
            return;
        }
        _isLoading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_gameSceneName); 
    }
    //기본 메뉴를 숨기고 조작법 표시
    public void OpenControls()
    {
        if(_isLoading || _controlsBox == null) return;
        if(_mainMenuBox != null)
            _mainMenuBox.SetActive(false);
        _controlsBox.SetActive(true);
    }
    //조작법을 숨기고 기본 메뉴 표시
    public void CloseControls()
    {
        if (_mainMenuBox != null)
            _mainMenuBox.SetActive(true);
        if(_controlsBox != null)
            _controlsBox.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        //에디터에서는 플레이 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
