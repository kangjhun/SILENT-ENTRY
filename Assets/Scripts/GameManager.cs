using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing, //게임 진행 중
        Success, //목표물 확보 후 탈출 성공
        Failure//적에게 붙잡혀 실패
    }

    [Header("현재 게임 상태")]
    [SerializeField] GameState _currentState = GameState.Playing;

    [Header("게임 종료 시 중단할 플레이어 조작")]
    [SerializeField] PlayerMovement _playerMovement;
    [SerializeField] PlayerInteraction _playerInteraction;

    bool _hasObjective;

    public GameState CurrentState => _currentState;
    public bool IsPlaying => _currentState == GameState.Playing;
    public bool HasObjective => _hasObjective;

     void Awake()
    {
        //씬을 새로 시작할 때 정지된 게임 시간을 복구
        Time.timeScale = 1f;
        //게임 진행 상태와 목표물 보유 여부 초기화
        _currentState = GameState.Playing;
        _hasObjective = false;
    }

    //목표물 획득 처리에 성공했는지 반환 
    public bool AcquiredObjective()
    {
        //게임이 끝났거나 이미 획득했다면 중복 처리 방지
        if (!IsPlaying || _hasObjective) return false;

        _hasObjective = true;

        Debug.Log("목표물 확보! 탈출 지점으로 이동하세요", this);
        return true;
    }

    //탈출 조건을 만족했는지 반환
    public bool TryEscape()
    {
        if (!IsPlaying) return false;

        //목표물이 없으면 탈출하지 못하고 게임을 계속 진행
        if (!_hasObjective)
        {
            Debug.Log("목표물을 먼저 확보하세요.", this);
            return false;
        }

        EndGame(GameState.Success);

        return true;
    }

    //EnemyFSM에서 플레이어를 붙잡았을 때 호출
    public void FailMission()
    { 
        EndGame(GameState.Failure);
    }

    void EndGame(GameState state)
    {
        if (!IsPlaying) return;

        _currentState = state;

        Debug.Log(state == GameState.Success ? "탈출 성공! 임무를 완료했습니다." : "적에게 붙잡혔습니다. 임무 실패!", this);

        DisablePlayerControls();
       
        //결과 화면에서 마우스를 사용할 수 있도록 커서 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //게임 시간 정지
        Time.timeScale = 0f;
    }

    void DisablePlayerControls()
    {
     if(_playerMovement != null) _playerMovement.enabled = false;
     if(_playerInteraction !=  null) _playerInteraction.enabled = false;
    }
}
