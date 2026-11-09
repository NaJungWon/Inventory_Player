using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_Manager : MonoBehaviour
{
    public enum GameState
    {
        None,
        Title,
        Robby,
        InGame
    }
    public GameState gameState;
    public static Game_Manager Instance { get; private set; } // 싱글톤 인스턴스
    [Header("인게임 스테이지 웨이브 데이터")]
    public Stage_Wave_Info currentStageWaveInfo;
    [Header("플레이어 데이터")]
    public Player_Info currentPlayerData;//현재 플레이어 데이터

    [Header("사운드 볼륨 값")]
    public float musicVolumeValue;
    public float sfxVolumeValue;
    public bool isMusicVolumeValueSet = false; // 음악 볼륨 값이 설정되었는지 확인
    public bool isSfxVolumeValueSet = false; // 효과음 볼륨 값이 설정되었는지 확인

    public User_Data userData;
    public float playTimeTimer = 0f; // 플레이 타임 타이머
    private const float PLAY_TIME_INTERVAL = 60f; // 60초마다 갱신
    void Awake()
    {
        // 싱글톤 패턴 - 중복 오브젝트 방지
        if (Instance == null)
        {
            Instance = this;
            // 씬 이동 시에도 오브젝트가 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 인스턴스가 있으면 중복 오브젝트 파괴
            Destroy(gameObject);
        }

        userData = GetComponent<User_Data>();
    }

    // Update is called once per frame
    void Update()
    {
        if(gameState == GameState.Robby || gameState == GameState.InGame)
        {
            // 플레이 타임 갱신
            UpdatePlayTime();
        }
    }

    // 플레이 타임 갱신 (60초마다 1씩 증가)
    private void UpdatePlayTime()
    {
        if (userData == null)
            return;

        playTimeTimer += Time.deltaTime;

        if (playTimeTimer >= PLAY_TIME_INTERVAL)
        {
            // 비동기로 플레이 타임 업데이트
            StartCoroutine(UpdatePlayTimeAsync());
            playTimeTimer = 0f; // 타이머 리셋
        }
    }

    // 플레이 타임 비동기 업데이트
    private IEnumerator UpdatePlayTimeAsync()
    {
        bool isLoaded = false;
        User_Data.PlayerData playerData = null;
        
        // Firebase에서 플레이어 데이터 가져오기
        userData.GetCurrentPlayerData().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                playerData = task.Result;
            }
            isLoaded = true;
        });

        // 로드 완료까지 대기
        while (!isLoaded)
        {
            yield return null;
        }

        if (playerData != null)
        {
            // 플레이 타임 60초 증가
            playerData.playTime += 60f;
            
            // Firebase에 저장
            userData.UpdatePlayerData(playerData);
            
            Debug.Log($"플레이 타임 갱신: {playerData.playTime}초");
        }
    }

    public void ExitGame()
    {
        Debug.Log("게임 종료");
        
        #if UNITY_EDITOR
        // 에디터에서 실행 중일 때는 플레이 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 빌드된 게임에서는 게임 종료
        Application.Quit();
        #endif
    }
}
