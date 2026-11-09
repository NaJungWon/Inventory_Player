using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Robby_System : MonoBehaviour
{
    public List<Stage_Wave_Info> stageWaveInfos;
    public List<Player_Info> playerCharacterIndex;
    public Game_Manager gameManager;
    public Robby_UI robbyUI;
    // Start is called before the first frame update
    void Start()
    {
        robbyUI = GetComponent<Robby_UI>();
        gameManager = Game_Manager.Instance;
        gameManager.gameState = Game_Manager.GameState.Robby;
        // 플레이어 데이터가 로드될 때까지 대기
        StartCoroutine(WaitForPlayerData());
    }

    private IEnumerator WaitForPlayerData()
    {
        // 플레이어 데이터가 로드될 때까지 대기
        while (gameManager.userData == null || string.IsNullOrEmpty(gameManager.userData.GetCurrentUID()))
        {
            yield return null;
        }

        // Firebase에서 플레이어 데이터 로드
        yield return StartCoroutine(LoadPlayerDataFromFirebase());

        // 플레이어 데이터가 로드되면 스테이지 설정
        yield return StartCoroutine(GetPlayerDataAndSetup());
    }

    // Firebase에서 플레이어 데이터 로드
    private IEnumerator LoadPlayerDataFromFirebase()
    {
        bool isLoaded = false;
        
        // Firebase에서 직접 데이터 가져오기
        gameManager.userData.GetCurrentPlayerData().ContinueWith(task =>
        {
            isLoaded = true;
        });

        // 로드 완료까지 대기
        while (!isLoaded)
        {
            yield return null;
        }
    }

    // 플레이어 데이터 가져오기 및 스테이지 설정
    private IEnumerator GetPlayerDataAndSetup()
    {
        bool isLoaded = false;
        User_Data.PlayerData playerData = null;
        
        // Firebase에서 플레이어 데이터 가져오기
        gameManager.userData.GetCurrentPlayerData().ContinueWith(task =>
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
            // 로드된 플레이어 데이터 출력
            Debug.Log($"=== 플레이어 데이터 로드 완료 ===");
            Debug.Log($"플레이어 이름: {playerData.user_name}");
            Debug.Log($"레벨: {playerData.level}");
            Debug.Log($"경험치: {playerData.exp}");
            Debug.Log($"골드: {playerData.gold}");
            Debug.Log($"스테이지: {playerData.stage}");
            Debug.Log($"플레이 시간: {playerData.playTime}시간");
            Debug.Log($"접속 횟수: {playerData.connectCount}");
            Debug.Log($"마지막 로그인: {playerData.lastLoginTime}");
            Debug.Log($"===============================");
            // 플레이어 UI 설정
            robbyUI.playerNameText.text = playerData.user_name;
            robbyUI.playerLevelText.text = playerData.level.ToString();
            // 스테이지 및 UI 설정
            int currentStage = playerData.stage;
            if (currentStage > 0 && currentStage <= stageWaveInfos.Count)
            {
                if(robbyUI.stageLockIcon.activeSelf)
                {
                    robbyUI.stageLockIcon.SetActive(false);
                }
                robbyUI.stageText.text = "Chapter. " + currentStage;
                gameManager.currentStageWaveInfo = stageWaveInfos[currentStage - 1];
                robbyUI.stageNameText.text = gameManager.currentStageWaveInfo.stageName;
                robbyUI.stageImage.sprite = robbyUI.stageSprites[currentStage - 1];

                Debug.Log($"스테이지 {currentStage} 웨이브 정보 설정 완료");
            }
            else if(currentStage > stageWaveInfos.Count)
            {
                robbyUI.stageText.text = "Chapter. " + currentStage;
                robbyUI.stageNameText.text = "Locked";
                robbyUI.stageImage.gameObject.SetActive(false);
                robbyUI.stageLockIcon.SetActive(true);
                robbyUI.goInGameButton.GetComponent<Button>().interactable = false; // 버튼 비활성화
                Debug.Log($"스테이지 {currentStage} 웨이브 정보 설정 완료");
            }

            int currentCharacter = playerData.player_Character_Index;
            if (currentCharacter >= 0 && currentCharacter < playerCharacterIndex.Count)
            {
                gameManager.currentPlayerData = playerCharacterIndex[currentCharacter];
                Debug.Log($"플레이어 캐릭터 {currentCharacter} 설정 완료");
            }
            else
            {
                Debug.LogError($"유효하지 않은 캐릭터 번호: {currentCharacter}");
            }
        }
        else
        {
            Debug.LogError("플레이어 데이터 로드 실패");
        }
    }

    public void GoInGame()
    {
        SceneManager.LoadScene("InGame");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
