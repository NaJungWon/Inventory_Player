using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;

public class InGame_UI : MonoBehaviour
{
    public InGame_System inGameSystem;
    public TextMeshProUGUI waveNumberText; //웨이브 번호 텍스트
    public TextMeshProUGUI itemDGText; //아이템 데미지 텍스트
    public TextMeshProUGUI itemHpText; //아이템 체력 텍스트
    public TextMeshProUGUI itemDpText; //아이템 방어력 텍스트
    public Slider waveSlider; //웨이브 진행 바
    public GameObject inventoryUI; //인벤 UI
    public GameObject inventorySlot; //사용중인 인벤 슬롯
    public GameObject useWeaponSlot; //전투중 사용중인 무기 슬롯
    public GameObject occupiedWeaponSlot; //장착된 무기 보관 오브젝트
    public GameObject reRollSlot; //리롤 슬롯
    public List<GameObject> ingameButtons; //전투 버튼,리롤 버튼
    public GameObject pauseButton; //일시정지 버튼
    public GameObject pauseUI; //일시정지 UI
    public GameObject gameClearUI; //게임클리어 UI
    public GameObject gameOverUI; //게임오버 UI
    public GameObject settingUI; //설정 UI
    public float settingUIAnimDuration = 0.35f; // 설정창 등장 시간
    private RectTransform inventoryRect; //가방좌표
    private Coroutine inventoryMoveCoroutine;
    private Coroutine buttonMoveCoroutine;
    private Coroutine sliderMoveCoroutine; // 슬라이더 이동 코루틴
    private const float moveDurationSeconds = 1f;
    private const float hiddenY = -1185f;
    private const float shownY = -120f;
    private const float buttonDefaultY = -850f;
    private const float buttonHiddenY = -950f;
    private InGame_System.InGameState previousInGameState; // 일시정지 전 게임 상태 저장
    private Transform _settingContent;
    private Coroutine _settingUICoroutine;
    private Coroutine _buttonAnimationCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        inGameSystem = GetComponent<InGame_System>();
        if (inventoryUI != null)
        {
            inventoryRect = inventoryUI.GetComponent<RectTransform>();
        }
        if (settingUI != null && settingUI.transform.childCount > 1)
        {
            _settingContent = settingUI.transform.GetChild(1);
        }
        inventorySlot.SetActive(false);
        useWeaponSlot.SetActive(false);
        reRollSlot.SetActive(false);
        pauseUI.SetActive(false);
        gameClearUI.SetActive(false);
        gameOverUI.SetActive(false);
        settingUI.SetActive(false);
        itemDGText.text = "0";
        itemHpText.text = "0 %";
        itemDpText.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //인벤토리 열기
    public void openInventory()
    {
        if (inventoryRect == null) return;
        // Deactivate whichever is active before moving
        if (reRollSlot != null && reRollSlot.activeSelf) reRollSlot.SetActive(false);
        if (useWeaponSlot != null && useWeaponSlot.activeSelf) useWeaponSlot.SetActive(false);
        if (inventorySlot != null && inventorySlot.activeSelf) inventorySlot.SetActive(false);
        if (occupiedWeaponSlot != null && occupiedWeaponSlot.activeSelf) occupiedWeaponSlot.SetActive(false);

        if (inventoryMoveCoroutine != null) StopCoroutine(inventoryMoveCoroutine);
        inventoryMoveCoroutine = StartCoroutine(MoveInventoryY(shownY, () =>
        {
            if (inventorySlot != null) inventorySlot.SetActive(true);
            if (reRollSlot != null) reRollSlot.SetActive(true);
            if (occupiedWeaponSlot != null) occupiedWeaponSlot.SetActive(true);
        }));
        
        // 버튼들을 기본 위치로 이동하고 스크립트 활성화
        MoveButtonsToDefault();
    }

    //인벤토리 숨기기
    public void hideInventory()
    {
        if (inventoryRect == null) return;
        // Deactivate whichever is active before moving
        if (reRollSlot != null && reRollSlot.activeSelf) reRollSlot.SetActive(false);
        if (useWeaponSlot != null && useWeaponSlot.activeSelf) useWeaponSlot.SetActive(false);
        if (inventorySlot != null && inventorySlot.activeSelf) inventorySlot.SetActive(false);
        if (occupiedWeaponSlot != null && occupiedWeaponSlot.activeSelf) occupiedWeaponSlot.SetActive(false);

        if (inventoryMoveCoroutine != null) StopCoroutine(inventoryMoveCoroutine);
        inventoryMoveCoroutine = StartCoroutine(MoveInventoryY(hiddenY, () =>
        {
            if (useWeaponSlot != null) useWeaponSlot.SetActive(true);
        }));
        
        // 버튼들을 숨김 위치로 이동하고 스크립트 비활성화
        MoveButtonsToHidden();
    }

    public void UpdateWaveNumberText()
    {
        waveNumberText.text = "Wave " + inGameSystem.currentWaveNumber.ToString() + " / " + inGameSystem.maxWaveNumber.ToString();
        
        // 슬라이더 값을 1초에 걸쳐 부드럽게 변경
        float targetValue = (float)inGameSystem.currentWaveNumber / inGameSystem.maxWaveNumber;
        if (sliderMoveCoroutine != null) StopCoroutine(sliderMoveCoroutine);
        sliderMoveCoroutine = StartCoroutine(MoveSliderValue(targetValue));
    }
    
    // 슬라이더 값을 부드럽게 변경하는 코루틴
    private IEnumerator MoveSliderValue(float targetValue)
    {
        if (waveSlider == null) yield break;
        
        float startValue = waveSlider.value;
        float elapsed = 0f;
        
        while (elapsed < moveDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDurationSeconds);
            waveSlider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }
        
        waveSlider.value = targetValue;
        sliderMoveCoroutine = null;
    }

    private IEnumerator MoveInventoryY(float targetY, Action onComplete = null)
    {
        Vector2 startPos = inventoryRect.anchoredPosition;
        float startY = startPos.y;
        float elapsed = 0f;
        while (elapsed < moveDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDurationSeconds);
            float newY = Mathf.Lerp(startY, targetY, EaseOutBounce(t));
            inventoryRect.anchoredPosition = new Vector2(startPos.x, newY);
            yield return null;
        }
        inventoryRect.anchoredPosition = new Vector2(startPos.x, targetY);
        inventoryMoveCoroutine = null;
        onComplete?.Invoke();
    }

    public float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    // 버튼들을 기본 위치로 이동하고 스크립트 활성화
    private void MoveButtonsToDefault()
    {
        if (buttonMoveCoroutine != null) StopCoroutine(buttonMoveCoroutine);
        buttonMoveCoroutine = StartCoroutine(MoveButtonsY(buttonDefaultY, true));
    }
    
    // 버튼들을 숨김 위치로 이동하고 스크립트 비활성화
    private void MoveButtonsToHidden()
    {
        if (buttonMoveCoroutine != null) StopCoroutine(buttonMoveCoroutine);
        buttonMoveCoroutine = StartCoroutine(MoveButtonsY(buttonHiddenY, false));
    }
    
    // 버튼들의 Y 좌표 이동과 스크립트 활성화/비활성화
    private IEnumerator MoveButtonsY(float targetY, bool enableScripts)
    {
        if (ingameButtons == null || ingameButtons.Count == 0) yield break;
        
        // 각 버튼의 시작 위치 저장
        Vector2[] startPositions = new Vector2[ingameButtons.Count];
        for (int i = 0; i < ingameButtons.Count; i++)
        {
            if (ingameButtons[i] != null)
            {
                RectTransform buttonRect = ingameButtons[i].GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    startPositions[i] = buttonRect.anchoredPosition;
                }
            }
        }
        
        float elapsed = 0f;
        while (elapsed < moveDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDurationSeconds);
            
            for (int i = 0; i < ingameButtons.Count; i++)
            {
                if (ingameButtons[i] != null)
                {
                    RectTransform buttonRect = ingameButtons[i].GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        float newY = Mathf.Lerp(startPositions[i].y, targetY, t);
                        buttonRect.anchoredPosition = new Vector2(startPositions[i].x, newY);
                    }
                }
            }
            yield return null;
        }
        
        // 최종 위치 설정 및 스크립트 활성화/비활성화
        for (int i = 0; i < ingameButtons.Count; i++)
        {
            if (ingameButtons[i] != null)
            {
                RectTransform buttonRect = ingameButtons[i].GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = new Vector2(startPositions[i].x, targetY);
                }
                
                // Button 컴포넌트 활성화/비활성화
                UnityEngine.UI.Button buttonComponent = ingameButtons[i].GetComponent<UnityEngine.UI.Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.enabled = enableScripts;
                }
            }
        }
        
        buttonMoveCoroutine = null;
    }

    public void GameClear()
    {
        inGameSystem.inGameState = InGame_System.InGameState.GameClear;
        gameClearUI.SetActive(true);
        
        // User_Data에서 플레이어 데이터 가져와서 stage 값 증가
        StartCoroutine(UpdatePlayerStage());
    }
    
    // 플레이어 stage 값 업데이트 코루틴
    private IEnumerator UpdatePlayerStage()
    {
        // Game_Manager를 통해 User_Data 가져오기
        if (Game_Manager.Instance == null || Game_Manager.Instance.userData == null)
        {
            Debug.LogWarning("Game_Manager 또는 User_Data를 찾을 수 없습니다.");
            yield break;
        }
        
        User_Data userData = Game_Manager.Instance.userData;
        
        // 현재 플레이어 데이터 가져오기
        var playerDataTask = userData.GetCurrentPlayerData();
        yield return new WaitUntil(() => playerDataTask.IsCompleted);
        
        User_Data.PlayerData currentData = playerDataTask.Result;
        if (currentData != null)
        {
            // stage 값 1 증가
            currentData.stage += 1;
            
            // 업데이트된 데이터 저장
            var updateTask = userData.UpdatePlayerData(currentData);
            yield return new WaitUntil(() => updateTask.IsCompleted);
            
            Debug.Log($"스테이지 업데이트 완료: {currentData.stage}");
        }
        else
        {
            Debug.LogWarning("플레이어 데이터를 가져올 수 없습니다.");
        }
    }

    public void GameOver()
    {
        inGameSystem.inGameState = InGame_System.InGameState.GameOver;
        gameOverUI.SetActive(true);
    }

    public void PauseButton()
    {
        // 일시정지 전 게임 상태 저장
        previousInGameState = inGameSystem.inGameState;
        inGameSystem.inGameState = InGame_System.InGameState.Pause;
        Time.timeScale = 0;
        pauseUI.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = inGameSystem.gameManager.GetComponent<Game_Manager>().currentStageWaveInfo.stageName + " " + inGameSystem.currentWaveNumber.ToString() + " / " + inGameSystem.maxWaveNumber.ToString();
        pauseUI.SetActive(true);
    }

    public void ReSumeButton()
    {
        pauseUI.SetActive(false);
        // 저장된 게임 상태로 복원
        inGameSystem.inGameState = previousInGameState;
        Time.timeScale = 1;
    }

    public void RePlayButton()
    {
        if(gameOverUI.activeSelf)
        {
            gameOverUI.SetActive(false);
        }
        pauseUI.SetActive(false);
        inGameSystem.inGameState = InGame_System.InGameState.Rest;
        inGameSystem.currentWaveNumber = 1;
        UpdateWaveNumberText();
        inGameSystem.isWaitingForMonstersToDie = false;
        inGameSystem.isProcessingCurrentWave = false;
        inGameSystem.inventorySystem.ResetInventoryToInitialState();
        
        // spawnedMonsters 리스트의 모든 몬스터 삭제
        if (inGameSystem.spawnedMonsters != null)
        {
            for (int i = inGameSystem.spawnedMonsters.Count - 1; i >= 0; i--)
            {
                if (inGameSystem.spawnedMonsters[i] != null)
                {
                    Destroy(inGameSystem.spawnedMonsters[i]);
                }
            }
            inGameSystem.spawnedMonsters.Clear();
        }
        
        inGameSystem.player.GetComponent<Unit_Status>().ResetUnitStatus();
        // Queue 초기화
        inGameSystem.InitializeSpawnDataQueue();
        //inGameSystem.inGameTimer = 0;
        Time.timeScale = 1;
        openInventory();
    }

    public void GoRobbyButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Robby");
    }

    public void OpenSettingUI()
    {
        if (settingUI == null || _settingContent == null) return;

        if (settingUI.activeSelf)
        {
            if (_settingUICoroutine != null)
            {
                StopCoroutine(_settingUICoroutine);
                _settingUICoroutine = null;
            }
            settingUI.SetActive(false);
            _settingContent.localScale = Vector3.one;
            return;
        }

        if (_settingUICoroutine != null)
        {
            StopCoroutine(_settingUICoroutine);
        }
        _settingUICoroutine = StartCoroutine(ShowSettingUIWithEase());
    }

    private IEnumerator ShowSettingUIWithEase()
    {
        settingUI.SetActive(true);
        _settingContent.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < settingUIAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settingUIAnimDuration);
            float eased = EaseOutBack(t);
            _settingContent.localScale = Vector3.one * eased;
            yield return null;
        }

        _settingContent.localScale = Vector3.one;
        _settingUICoroutine = null;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
    
    // 버튼 클릭 시 EaseOutBack 애니메이션 실행
    public void PlayButtonEaseOutBackAnimation(GameObject targetButton)
    {
        if (targetButton == null) return;
        
        // 기존 애니메이션이 실행 중이면 중지
        if (_buttonAnimationCoroutine != null)
        {
            StopCoroutine(_buttonAnimationCoroutine);
        }
        
        // 새 애니메이션 시작
        _buttonAnimationCoroutine = StartCoroutine(ButtonEaseOutBackCoroutine(targetButton));
    }
    
    private IEnumerator ButtonEaseOutBackCoroutine(GameObject targetButton)
    {
        if (targetButton == null) yield break;
        
        Transform targetTransform = targetButton.transform;
        Vector3 defaultScale = Vector3.one;
        Vector3 startScale = defaultScale * 0.9f;
        Vector3 peakScale = defaultScale * 1.1f;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        // 시작: 작은 크기
        targetTransform.localScale = startScale;
        
        while (elapsed < duration)
        {
            if (targetButton == null) yield break;
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // 피크를 지나면서 정상 크기로 복귀
            if (t < 0.6f)
            {
                // 피크까지 확대
                float peakT = t / 0.6f;
                float peakEased = EaseOutBack(peakT);
                targetTransform.localScale = Vector3.Lerp(startScale, peakScale, peakEased);
            }
            else
            {
                // 피크에서 정상 크기로 복귀
                float returnT = (t - 0.6f) / 0.4f;
                float returnEased = EaseOutBack(returnT);
                targetTransform.localScale = Vector3.Lerp(peakScale, defaultScale, returnEased);
            }
            
            yield return null;
        }
        
        if (targetButton != null)
        {
            targetTransform.localScale = defaultScale;
        }
        _buttonAnimationCoroutine = null;
    }
    
    // 장착 아이템 통계에 따라 UI 업데이트
    public void UpdateItemStatsUI()
    {
        if (inGameSystem == null || inGameSystem.inventorySystem == null) return;
        if (inGameSystem.player == null) return;
        
        Unit_Status playerStatus = inGameSystem.player.GetComponent<Unit_Status>();
        if (playerStatus == null) return;
        
        int totalDamage = 0;
        int totalHeal = 0;
        
        // OccupiedItemDatas 순회하며 통계 계산
        if (inGameSystem.inventorySystem.OccupiedItemDatas != null)
        {
            foreach (var record in inGameSystem.inventorySystem.OccupiedItemDatas)
            {
                if (record == null || record.itemData == null) continue;
                
                // Throw 또는 Shoot 타입이면 데미지 합산
                if (record.itemData.weaponType == Item_Data.WeaponType.Throw || 
                    record.itemData.weaponType == Item_Data.WeaponType.Shoot)
                {
                    totalDamage += record.itemValue;
                }
                // Heal 타입이면 회복량 합산
                else if (record.itemData.weaponType == Item_Data.WeaponType.Heal)
                {
                    totalHeal += record.itemValue;
                }
            }
        }
        
        // 데미지 텍스트 업데이트
        if (itemDGText != null)
        {
            itemDGText.text = totalDamage.ToString();
        }
        
        // 회복량 텍스트 업데이트 (플레이어 maxHp 대비 퍼센트)
        if (itemHpText != null && playerStatus.maxHp > 0)
        {
            float healPercent = (float)totalHeal / playerStatus.maxHp * 100f;
            itemHpText.text = healPercent.ToString("F1") + " %";
        }
        else if (itemHpText != null)
        {
            itemHpText.text = "0 %";
        }
    }
}
