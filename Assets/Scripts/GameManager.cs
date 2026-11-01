using UnityEngine;
//목표물 획득, 탈출, 실패 등 게임 진행 관리
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing, //게임 진행 중
        Success, //탈출 성공
        Failure//적에게 붙잡혀 실패
    }

    [Header("현재 게임 상태")]
    [SerializeField] GameState _currentState = GameState.Playing;

    [Header("게임 종료 시 중단할 플레이어 조작")]
    [SerializeField] PlayerMovement _playerMovement;
    [SerializeField] PlayerInteraction _playerInteraction;

    [Header("화면 안내")]
    [SerializeField] GameUIManager _gameUIManager;

    bool _hasObjective;

    public GameState CurrentState => _currentState;
    public bool IsPlaying => _currentState == GameState.Playing;
    public bool HasObjective => _hasObjective;

     void Awake()
    {
        //게임 시간과 진행 상태 초기화
        Time.timeScale = 1f;
        _currentState = GameState.Playing;
        _hasObjective = false;
    }

    //목표물 획득 처리에 성공하면 true 반환 
    public bool AcquiredObjective()
    {
        //게임 종료 후 획득하거나 중복 획득하는 것 방지
        if (!IsPlaying || _hasObjective) return false;

        _hasObjective = true;

        Debug.Log("목표물 확보! 탈출 지점으로 이동하세요", this);
        return true;
    }

   
    public bool TryEscape()
    {
        if (!IsPlaying) return false;

        //목표물이 없으면 안내 문구를 띄우고 탈출 차단
        if (!_hasObjective)
        {
            if (_gameUIManager != null)
                _gameUIManager.ShowNotice("목표물을 먼저 확보하세요.");
            return false;
        }

        EndGame(GameState.Success);

        return true;
    }

    //적에게 붙잡혔을 때 실패 처리
    public void FailMission()
    { 
        EndGame(GameState.Failure);
    }

    void EndGame(GameState state)
    {
        //게임 종료가 중복 처리되지 않도록 확인
        if (!IsPlaying) return;

        _currentState = state;

        Debug.Log(state == GameState.Success ? "탈출 성공! 임무를 완료했습니다." : "적에게 붙잡혔습니다. 임무 실패!", this);

        DisablePlayerControls();
       
        //결과 화면에서 마우스를 사용할 수 있도록 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //게임 시간 정지
        Time.timeScale = 0f;
    }

    void DisablePlayerControls()
    {
        //플레이어 이동과 상호작용 중단
     if(_playerMovement != null) _playerMovement.enabled = false;
     if(_playerInteraction !=  null) _playerInteraction.enabled = false;
    }
}
