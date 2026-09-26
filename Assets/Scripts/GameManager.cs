using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Success,
        Failure
    }

    [SerializeField] GameState _currentState = GameState.Playing;

    [SerializeField] PlayerMovement _playerMovement;
    [SerializeField] PlayerLook _playerLook;
    [SerializeField] PlayerInteraction _playerInteraction;

    bool _hasObjective;

    public GameState CurrentState => _currentState;
    public bool IsPlaying => _currentState == GameState.Playing;
    public bool HasObjective => _hasObjective;

     void Awake()
    {
        Time.timeScale = 1f;
        _currentState = GameState.Playing;
        _hasObjective = false;
    }

    public bool AcquiredObjective()
    {
        if (!IsPlaying || _hasObjective) return false;

        _hasObjective = true;

        Debug.Log("목표물 확보! 탈출 지점으로 이동하세요", this);
        return true;
    }

    public bool TryEscape()
    {
        if (!IsPlaying) return false;

        if (!_hasObjective)
        {
            Debug.Log("목표물을 먼저 확보하세요.", this);
            return false;
        }

        EndGame(GameState.Success);

        return true;
    }

    public void FailMission()
    {
        if (!IsPlaying) return;
        EndGame(GameState.Failure);
    }

    void EndGame(GameState state)
    {
        if (!IsPlaying) return;

        _currentState = state;

        Debug.Log(state == GameState.Success ? "탈출 성공! 임무를 완료했습니다." : "적에게 붙잡혔습니다. 임무 실패!", this);

        if(_playerMovement != null) _playerMovement.enabled = false;
        if(_playerLook != null) _playerLook.enabled = false;
        if(_playerInteraction !=  null) _playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }
}
