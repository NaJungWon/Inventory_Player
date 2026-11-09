using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGame_System : MonoBehaviour
{
    public enum InGameState
    {
        None,
        Fight,
        Rest,
        Pause,
        GameClear,
        GameOver
    }
    public InGameState inGameState;
    public InGame_UI inGameUI;
    public GameObject gameManager;
    public Stage_Wave_Info stageInfo;
    [Header("스테이지/현재 웨이브/최대 웨이브")]
    public int stageNumber;
    public int currentWaveNumber;
    public int maxWaveNumber;
    [Header("플레이어/몬스터 생성 위치/생성된 몬스터 리스트")]
    public GameObject player;
    public GameObject monsterSpawnPosition;
    public List<GameObject> monsterList;

    [Header("사용중인 아이템 설정")]
    public Inventory_System inventorySystem;
    public GameObject occupiedWeaponSlot; //사용중인 무기 기본 슬롯
    public List<GameObject> activeWeaponSlots = new List<GameObject>(); // 활성화된 전투용 무기 슬롯들
    public List<GameObject> spawnedMonsters = new List<GameObject>(); // 생성된 몬스터들

    public float inGameTimer;
    public Queue<Stage_Wave_Info.MonsterSpawnData> spawnDataQueue = new Queue<Stage_Wave_Info.MonsterSpawnData>(); // Queue 방식으로 처리할 spawnData
    public bool isWaitingForMonstersToDie = false; // 몬스터들이 모두 사망할 때까지 대기 중인지
    public bool isProcessingCurrentWave = false; // 현재 웨이브 처리 중인지
    public AudioSource inGameAudio;
    
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager");
        gameManager.GetComponent<Game_Manager>().gameState = Game_Manager.GameState.InGame;
        inGameUI = GetComponent<InGame_UI>();
        stageInfo = gameManager.GetComponent<Game_Manager>().currentStageWaveInfo;

        inGameState = InGameState.Rest;
        stageNumber = stageInfo.stageNumber;
        currentWaveNumber = 1;
        maxWaveNumber = stageInfo.maxWaveNumber;
        inGameTimer = 0;
        isWaitingForMonstersToDie = false;
        isProcessingCurrentWave = false;

        PlayerDataInput(gameManager.GetComponent<Game_Manager>().currentPlayerData);
        inGameUI.UpdateWaveNumberText();
        // Queue 초기화
        InitializeSpawnDataQueue();
        
        // Inventory_System에 InGame_System 참조 설정
        if (inventorySystem != null)
        {
            inventorySystem.inGameSystem = this;
            // 초기 UI 업데이트
            inventorySystem.inGameSystem.inGameUI.UpdateItemStatsUI();
        }
        
        inGameUI.openInventory();
    }

    // Update is called once per frame
    void Update()
    {
        if(inGameState == InGameState.Fight)
        {
            inGameTimer += Time.deltaTime;
            
            // 몬스터 소환 로직 처리
            ProcessMonsterSpawning();
            
            // 몬스터 사망 체크 및 리스트 정리
            CleanupDeadMonsters();
        }

        if(inGameState == InGameState.Fight || inGameState == InGameState.Rest)
        {
            if(!inGameAudio.isPlaying)
            {
                inGameAudio.Play();
            }
        }
        else
        {
            inGameAudio.Stop();
        }
    }

    public void PlayerDataInput(Player_Info playerData)
    {
        player.GetComponent<Unit_Status>().inGameSystem = this;
        player.GetComponent<Unit_Status>().maxHp = playerData.playerHp;
        player.GetComponent<Unit_Status>().unitHp = playerData.playerHp;
        player.GetComponent<Unit_Status>().unitDamage = playerData.playerDamage;
        player.GetComponent<Unit_Status>().SetHpSlider();
    }

    // Queue 초기화
    public void InitializeSpawnDataQueue()
    {
        spawnDataQueue.Clear();
        
        // 모든 spawnData를 웨이브 번호와 시간 순으로 정렬하여 Queue에 추가
        List<Stage_Wave_Info.MonsterSpawnData> sortedSpawnData = new List<Stage_Wave_Info.MonsterSpawnData>(stageInfo.spawnDataList);
        sortedSpawnData.Sort((a, b) => 
        {
            // 먼저 웨이브 번호로 정렬
            int waveComparison = a.currentWaveNumber.CompareTo(b.currentWaveNumber);
            if (waveComparison != 0)
                return waveComparison;
            
            // 같은 웨이브 내에서는 시간 순으로 정렬
            return a.triggerTime.CompareTo(b.triggerTime);
        });
        
        foreach (var spawnData in sortedSpawnData)
        {
            spawnDataQueue.Enqueue(spawnData);
        }
        /*
        Debug.Log($"Queue 초기화 완료. 총 {spawnDataQueue.Count}개의 spawnData 로드됨");
        
        // 정렬된 데이터 확인을 위한 디버그 로그
        foreach (var data in sortedSpawnData)
        {
            Debug.Log($"정렬된 데이터: 웨이브 {data.currentWaveNumber}, 시간 {data.triggerTime}, 몬스터 {data.monsterInfo?.monsterName}");
        }
        */
    }
    
    // 몬스터 소환 로직 처리 (Queue 방식)
    void ProcessMonsterSpawning()
    {
        // Fight 상태가 아니면 몬스터 소환하지 않음
        if (inGameState != InGameState.Fight)
        {
            return;
        }
        
        // 몬스터들이 모두 사망할 때까지 대기 중이면 소환하지 않음
        if (isWaitingForMonstersToDie)
        {
            return;
        }
        
        // Queue가 비어있으면 더 이상 처리할 데이터가 없음
        if (spawnDataQueue.Count == 0)
        {
            // 현재 웨이브 처리 중이었다면 처리 중단
            if (isProcessingCurrentWave)
            {
                isWaitingForMonstersToDie = true;
                isProcessingCurrentWave = false;
                Debug.Log($"웨이브 {currentWaveNumber} 처리 중단. 몬스터들이 모두 사망할 때까지 대기 중... (Queue 비어있음)");
            }
            return;
        }
        
        // Queue의 첫 번째 데이터 확인
        Stage_Wave_Info.MonsterSpawnData nextSpawnData = spawnDataQueue.Peek();
        
        // 현재 웨이브와 다른 웨이브의 데이터라면 처리 중단
        if (nextSpawnData.currentWaveNumber != currentWaveNumber)
        {
            if (isProcessingCurrentWave)
            {
                // 현재 웨이브 처리 중단하고 몬스터들이 모두 사망할 때까지 대기
                isWaitingForMonstersToDie = true;
                isProcessingCurrentWave = false;
                Debug.Log($"웨이브 {currentWaveNumber} 처리 중단. 몬스터들이 모두 사망할 때까지 대기 중...");
            }
            return;
        }
        
        // 현재 시간에 맞는 spawnData인지 확인
        // 시각을 지났으면 스폰 (±오차 비교는 프레임이 끊기면 시각을 건너뛰어 웨이브가 멈춤)
        if (inGameTimer >= nextSpawnData.triggerTime)
        {
            // Queue에서 데이터 제거하고 몬스터 소환
            Stage_Wave_Info.MonsterSpawnData spawnData = spawnDataQueue.Dequeue();
            SpawnMonsters(spawnData);
            isProcessingCurrentWave = true;
            
            Debug.Log($"몬스터 소환: {spawnData.monsterInfo.monsterName} (웨이브 {currentWaveNumber}, 시간 {spawnData.triggerTime})");
        }
    }
    
    
    // 몬스터 소환 실행
    void SpawnMonsters(Stage_Wave_Info.MonsterSpawnData spawnData)
    {
        if (spawnData.monsterInfo == null || monsterSpawnPosition == null)
        {
            Debug.LogWarning("MonsterInfo 또는 SpawnPosition이 설정되지 않았습니다.");
            return;
        }
        
        for (int i = 0; i < spawnData.MonsterCount; i++)
        {
            // 몬스터 프리팹 생성
            GameObject monsterPrefab = spawnData.monsterInfo.monster;
            if (monsterPrefab != null)
            {
                // x좌표는 정중앙, y좌표는 랜덤
                Vector3 spawnPosition = monsterSpawnPosition.transform.position;
                spawnPosition.y += Random.Range(-2f, 3f); // y좌표 랜덤 범위 조정 가능

                // 몬스터 생성 (부모 지정하지 않음)
                // 먼저 생성한 뒤 인스턴스에 값을 설정 (프리팹 원본 에셋을 수정하지 않도록)
                GameObject spawnedMonster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
                Unit_Status unitStatus = spawnedMonster.GetComponent<Unit_Status>();
                UnityEngine.AI.NavMeshAgent agent = spawnedMonster.GetComponent<UnityEngine.AI.NavMeshAgent>();

                unitStatus.inGameSystem = this;
                spawnedMonster.GetComponent<Monster_Setting>().player = player;
                // 웨이브별 HP 증가: 기본값 + (웨이브 번호 - 1) * 10%
                float baseHp = spawnData.monsterInfo.monsterHp;
                float waveBonus = baseHp * 0.1f * (currentWaveNumber - 1);
                float finalMaxHp = baseHp + waveBonus;
                unitStatus.maxHp = (int)finalMaxHp;
                unitStatus.unitHp = unitStatus.maxHp;
                float baseDamage = spawnData.monsterInfo.monsterDamage;
                float waveDamageBonus = baseDamage * 0.1f * (currentWaveNumber - 1);
                float finalDamage = baseDamage + waveDamageBonus;
                unitStatus.unitDamage = (int)finalDamage;
                agent.speed = spawnData.monsterInfo.monsterSpeed;
                agent.stoppingDistance = spawnData.monsterInfo.attackRange;
                unitStatus.SetHpSlider();

                // spawnedMonsters 리스트에 추가
                spawnedMonsters.Add(spawnedMonster);
                // monsterList에도 추가 (기존 로직 유지)
                monsterList.Add(spawnedMonster);
            }
        }
    }
    
    // 몬스터 사망 체크 및 리스트 정리
    void CleanupDeadMonsters()
    {
        // null이거나 파괴된 몬스터들을 리스트에서 제거
        for (int i = monsterList.Count - 1; i >= 0; i--)
        {
            if (monsterList[i] == null)
            {
                monsterList.RemoveAt(i);
            }
        }
        
        // spawnedMonsters 리스트에서도 null 제거 (독립적으로 처리)
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }
        
        // 몬스터들이 모두 사망했고 대기 중이었다면 Rest 상태로 전환
        if (isWaitingForMonstersToDie && monsterList.Count == 0)
        {
            // Rest 상태로 전환
            inGameState = InGameState.Rest;
            inGameTimer = 0f;
            isWaitingForMonstersToDie = false;
            isProcessingCurrentWave = false;
            
            if (currentWaveNumber < maxWaveNumber)
            {
                currentWaveNumber++;
                inGameUI.UpdateWaveNumberText();
                inGameUI.openInventory();
                inventorySystem.RerollWeapon();
                Debug.Log($"웨이브 {currentWaveNumber} 준비 완료. Rest 상태로 전환. 다음 웨이브 시작 대기 중...");
                // 전투용 무기 슬롯들 정리
                ClearCombatWeaponSlots();
            }
            else if (currentWaveNumber == maxWaveNumber)
            {
                // 모든 웨이브 완료 - GameClear 상태로 전환
                inGameState = InGameState.GameClear;
                inGameUI.GameClear();
                Debug.Log("모든 웨이브 완료! GameClear 상태로 전환되었습니다.");
            }
        }
    }
    
    // 다음 웨이브 시작 (Rest 상태에서 Fight 상태로 전환)
    public void StartWave()
    {
        if (inGameState == InGameState.Rest && currentWaveNumber <= maxWaveNumber)
        {
            inGameState = InGameState.Fight;
            inGameTimer = 0f;
            isProcessingCurrentWave = false;
            
            // 전투용 무기 슬롯들 생성
            OccupiedWeaponSetting(inventorySystem);
            
            Debug.Log($"웨이브 {currentWaveNumber} 시작!");
        }
        else if (currentWaveNumber > maxWaveNumber)
        {
            Debug.Log("모든 웨이브가 완료되었습니다.");
        }
        else
        {
            Debug.Log("현재 Rest 상태가 아니거나 웨이브를 시작할 수 없습니다.");
        }
    }
    // 기존 전투용 무기 슬롯들 정리
    void ClearCombatWeaponSlots()
    {
        foreach (GameObject weaponSlot in activeWeaponSlots)
        {
            if (weaponSlot != null)
            {
                Destroy(weaponSlot);
            }
        }
        activeWeaponSlots.Clear();
    }
    
    //사용중인 무기 설정
    public void OccupiedWeaponSetting(Inventory_System inventorySystem)
    {
        if (inventorySystem == null || inventorySystem.OccupiedItemDatas == null)
        {
            Debug.LogWarning("InventorySystem 또는 OccupiedItemDatas가 설정되지 않았습니다.");
            return;
        }
        
        if (occupiedWeaponSlot == null)
        {
            Debug.LogWarning("occupiedWeaponSlotb이 설정되지 않았습니다.");
            return;
        }
        
        // 기존 전투용 무기 슬롯들 정리
        ClearCombatWeaponSlots();
        
        // 장착된 아이템 데이터 개수만큼 전투용 슬롯 생성
        for (int i = 0; i < inventorySystem.OccupiedItemDatas.Count; i++)
        {
            var itemData = inventorySystem.OccupiedItemDatas[i];
            
            // 전투용 무기 슬롯 프리팹 생성 (occupiedWeaponSlot을 부모로)
            GameObject weaponSlot = Instantiate(occupiedWeaponSlot, inGameUI.useWeaponSlot.transform);
            activeWeaponSlots.Add(weaponSlot);
            
            // Item_Function 컴포넌트 설정
            Item_Function itemFunction = weaponSlot.GetComponent<Item_Function>();
            if (itemFunction != null)
            {
                itemFunction.equippedItemRecord = itemData;
                itemFunction.player = player;
                itemFunction.itemValue = itemData.itemValue;
                itemFunction.inGameSystem = this;
                
                // 무기 타입 설정 (Item_Data에서 가져오기)
                if (itemData.itemData != null)
                {
                    itemFunction.weaponType = itemData.itemData.weaponType;
                }
            }
            
            Debug.Log($"전투용 무기 슬롯 생성: {itemData.itemName} (레벨 {itemData.itemLevel})");
        }
        
        Debug.Log($"총 {activeWeaponSlots.Count}개의 전투용 무기 슬롯이 생성되었습니다.");
    }
}
