using UnityEngine;
using UnityEngine.SceneManagement;
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
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CloseControls();
    }
    void Update()
    {
        if (_isLoading) return;
        if (Input.GetKeyDown(KeyCode.Escape) && _controlsBox != null && _controlsBox.activeSelf)
        {
            CloseControls();
        }
    }

    public void StartGame()
    {
        if(_isLoading) return;
        if (string.IsNullOrWhiteSpace(_gameSceneName))
        {
            Debug.LogError("시작할 게임 씬 이름을 입력하세요.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
        {
            Debug.LogError($"'{_gameSceneName}' 씬을 불러올 수 없습니다. " + "씬 이름과 빌드 씬 목록을 확인하세요.",this);
            return;
        }
        _isLoading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_gameSceneName); 
    }

    public void OpenControls()
    {
        if(_isLoading || _controlsBox == null) return;
        if(_mainMenuBox != null)
            _mainMenuBox.SetActive(false);
        _controlsBox.SetActive(true);
    }

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
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
