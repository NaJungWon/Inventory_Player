using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory_System : MonoBehaviour
{
    [Header("모든 아이템 데이터")]
    public List<Item_Data> itemDatas;

    [System.Serializable]
    public class EquippedItemRecord
    {
        public Item_Data itemData;
        public string itemName;
        public int itemLevel;
        public int itemValue;
        public float itemCooldown;
    }
    [Header("장착한 아이템 번들 데이터")]
    public List<EquippedItemRecord> OccupiedItemDatas;

    public GameObject inventoryBag;
    public GameObject occupiedWeaponSlot;
    public AudioSource occupiedSound;
    public GameObject weaponSlot;
    public int inventoryMaxWidth = 5;
    public int inventoryMaxHeight = 7;
    public SlotState[,] slots;
    public InGame_System inGameSystem; // InGame_System 참조

    public enum SlotState
    {
        None,
        Empty,
        Locked,
        Occupied
    }
    // Start is called before the first frame update
    void Start()
    {
        InitializeInventory();
        PrintSlots();
        occupiedSound = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //최초 인벤토리 상태 설정
    public void InitializeInventory()
    {
        slots = new SlotState[inventoryMaxWidth, inventoryMaxHeight];

        // inventoryBag의 모든 자식 오브젝트 가져오기
        Transform[] childSlots = new Transform[inventoryBag.transform.childCount];
        for (int i = 0; i < inventoryBag.transform.childCount; i++)
        {
            childSlots[i] = inventoryBag.transform.GetChild(i);
        }

        // 기본 잠김 상태로 초기화
        for (int x = 0; x < inventoryMaxWidth; x++)
            for (int y = 0; y < inventoryMaxHeight; y++)
                slots[x, y] = SlotState.Locked;

        // 열려있는 칸 설정 (2,3~4,5)
        for (int x = 1; x <= 3; x++)  // 2~4 칸, 배열 인덱스 = 1~3
            for (int y = 2; y <= 4; y++) // 3~5 칸, 배열 인덱스 = 2~4
                slots[x, y] = SlotState.Empty;

        // 자식 오브젝트들을 슬롯과 동기화
        for (int i = 0; i < childSlots.Length; i++)
        {
            // Grid Layout Group으로 정렬되어 있으므로 5개 단위로 행/열 계산
            int x = i % inventoryMaxWidth;
            int y = i / inventoryMaxWidth;

            // 배열 범위 체크
            if (x < inventoryMaxWidth && y < inventoryMaxHeight)
            {
                // Slot_State 컴포넌트 가져오기
                Slot_State slotStateComponent = childSlots[i].GetComponent<Slot_State>();
                if (slotStateComponent != null)
                {
                    // 슬롯 상태 동기화
                    slotStateComponent.slotState = (Slot_State.SlotState)slots[x, y];
                    
                    // 텍스트에 상태 표시 (주석처리)
                    /*
                    if (slotStateComponent.text != null)
                    {
                        switch ((Slot_State.SlotState)slots[x, y])
                        {
                            case Slot_State.SlotState.None:
                                slotStateComponent.text.text = "None";
                                break;
                            case Slot_State.SlotState.Empty:
                                slotStateComponent.text.text = "Empty";
                                break;
                            case Slot_State.SlotState.Locked:
                                slotStateComponent.text.text = "Locked";
                                break;
                            case Slot_State.SlotState.Occupied:
                                slotStateComponent.text.text = "Occupied";
                                break;
                        }
                    }
                    */
                }
            }
        }
        
        // 슬롯 활성화/비활성화 상태 초기화
        UpdateSlotActivation();
        
        // 가방 크기 조절
        AdjustInventoryBagSize();
        
        // 최초 무기 리롤 1회
        RerollWeapon();
    }

    // 슬롯 상태 출력 (디버깅용)
    public void PrintSlots()
    {
        for (int y = inventoryMaxHeight - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < inventoryMaxWidth; x++)
                row += slots[x, y] == SlotState.Locked ? "L " :
                       slots[x, y] == SlotState.Empty ? "E " : "O ";
            Debug.Log(row);
        }
    }

    public void RerollWeapon()
    {
        if (weaponSlot == null) return;
        if (itemDatas == null || itemDatas.Count == 0) return;

        // 1) 기존 자식 모두 제거
        for (int i = weaponSlot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = weaponSlot.transform.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // 2) 랜덤으로 4개 선택 (중복 허용)
        int spawnCount = 4;
        if (itemDatas.Count <= 0) return;

        List<int> chosen = new List<int>();
        for (int i = 0; i < spawnCount; i++)
        {
            int idx = Random.Range(0, itemDatas.Count);
            chosen.Add(idx);
        }

        // 3) 선택한 데이터의 item 프리팹을 weaponSlot의 자식으로 생성
        foreach (int idx in chosen)
        {
            Item_Data data = itemDatas[idx];
            if (data == null || data.item == null) continue;

            GameObject go = Instantiate(data.item, weaponSlot.transform);
            // 생성 후 기본 트랜스폼 정리
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
            }

            // Item_System이 있다면 데이터 동기화
            var itemSystem = go.GetComponent<Item_System>();
            if (itemSystem != null)
            {
                itemSystem.itemData = data;
                itemSystem.itemName = data.itemName;
                itemSystem.itemLevel = data.itemLevel;
                itemSystem.itemValue = data.itemValue;
                itemSystem.itemType = data.itemType;
                itemSystem.itemCooldown = data.itemCooldown;
                
                // itemType이 Item일 때만 이미지를 레벨에 맞게 변경
                if (itemSystem.itemType == Item_Data.ItemType.Item)
                {
                    go.transform.GetChild(1).GetComponent<Image>().sprite = data.itemImages[data.itemLevel - 1];
                }
            }
        }
    }
    
    // 가방 크기 조절 메서드
    public void AdjustInventoryBagSize(bool useAllSlots = false)
    {
        if (inventoryBag == null) return;
        
        float newWidth, newHeight;
        
        if (useAllSlots)
        {
            // 전체 인벤토리 크기 사용 (Slot 아이템 드래그 시)
            newWidth = (inventoryMaxWidth + 1) * 100f;
            newHeight = (inventoryMaxHeight + 1) * 100f;
            Debug.Log($"전체 슬롯 기준 가방 크기: {inventoryMaxWidth}x{inventoryMaxHeight} -> {newWidth}x{newHeight}");
        }
        else
        {
            // 활성화된 슬롯들의 경계 계산
            int minX = inventoryMaxWidth, maxX = -1;
            int minY = inventoryMaxHeight, maxY = -1;
            
            // Empty 또는 Occupied 슬롯들 찾기
            for (int x = 0; x < inventoryMaxWidth; x++)
            {
                for (int y = 0; y < inventoryMaxHeight; y++)
                {
                    if (slots[x, y] == SlotState.Empty || slots[x, y] == SlotState.Occupied)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            
            // 활성화된 슬롯이 없으면 기본 3x3 영역 사용
            if (maxX == -1)
            {
                minX = 1; maxX = 3;
                minY = 2; maxY = 4;
            }
            
            // 실제 사용되는 가로, 세로 크기 계산
            int usedWidth = maxX - minX + 1;
            int usedHeight = maxY - minY + 1;
            
            // inventoryBag 크기 설정 (가로+1)*100, (세로+1)*100
            newWidth = (usedWidth + 1) * 100f;
            newHeight = (usedHeight + 1) * 100f;
            Debug.Log($"가방 크기 조절: {usedWidth}x{usedHeight} -> {newWidth}x{newHeight}");
        }
        
        RectTransform bagRect = inventoryBag.GetComponent<RectTransform>();
        if (bagRect != null)
        {
            bagRect.sizeDelta = new Vector2(newWidth, newHeight);
        }
    }
    
    // 슬롯 활성화/비활성화 상태 업데이트
    public void UpdateSlotActivation()
    {
        if (inventoryBag == null) return;
        
        for (int i = 0; i < inventoryBag.transform.childCount; i++)
        {
            Transform slotTransform = inventoryBag.transform.GetChild(i);
            Slot_State slotStateComponent = slotTransform.GetComponent<Slot_State>();
            
            if (slotStateComponent != null)
            {
                int x = i % inventoryMaxWidth;
                int y = i / inventoryMaxWidth;
                
                if (x < inventoryMaxWidth && y < inventoryMaxHeight)
                {
                    SlotState currentState = slots[x, y];
                    
                    // Locked 슬롯은 기본적으로 비활성화
                    bool shouldBeActive = (currentState != SlotState.Locked);
                    slotStateComponent.gameObject.SetActive(shouldBeActive);
                    
                    // 슬롯 색상 설정
                    if (slotStateComponent.image != null)
                    {
                        Color slotColor = GetSlotColor(currentState, slotStateComponent);
                        slotStateComponent.image.color = slotColor;
                    }
                }
            }
        }
    }
    
    // Slot 아이템 드래그 시 모든 슬롯 활성화 (드래그 중 미리보기용)
    public void ActivateAllSlotsForSlotItem()
    {
        if (inventoryBag == null) return;
        
        // 모든 슬롯 활성화 (드래그 중 미리보기용)
        for (int i = 0; i < inventoryBag.transform.childCount; i++)
        {
            Transform slotTransform = inventoryBag.transform.GetChild(i);
            Slot_State slotStateComponent = slotTransform.GetComponent<Slot_State>();
            
            if (slotStateComponent != null)
            {
                slotStateComponent.gameObject.SetActive(true);
                
                int x = i % inventoryMaxWidth;
                int y = i / inventoryMaxWidth;
                
                if (x < inventoryMaxWidth && y < inventoryMaxHeight)
                {
                    SlotState currentState = slots[x, y];
                    
                    // 슬롯 색상 설정
                    if (slotStateComponent.image != null)
                    {
                        Color slotColor = GetSlotColor(currentState, slotStateComponent);
                        slotStateComponent.image.color = slotColor;
                    }
                }
            }
        }
        
        // 전체 인벤토리 크기로 조절
        AdjustInventoryBagSize(true);
    }
    
    // 슬롯 아이템 성공적 장착 후 활성화된 범위 재계산 및 가방 크기 조절
    public void UpdateInventoryAfterSlotPlacement()
    {
        if (inventoryBag == null) return;
        
        // 활성화된 슬롯들의 경계 계산
        int minX = inventoryMaxWidth, maxX = -1;
        int minY = inventoryMaxHeight, maxY = -1;
        
        // Empty 또는 Occupied 슬롯들 찾기
        for (int x = 0; x < inventoryMaxWidth; x++)
        {
            for (int y = 0; y < inventoryMaxHeight; y++)
            {
                if (slots[x, y] == SlotState.Empty || slots[x, y] == SlotState.Occupied)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        
        // 활성화된 슬롯이 없으면 기본 3x3 영역 사용
        if (maxX == -1)
        {
            minX = 1; maxX = 3;
            minY = 2; maxY = 4;
        }
        
        // 활성화된 범위 내의 모든 슬롯 활성화
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int slotIndex = y * inventoryMaxWidth + x;
                if (slotIndex < inventoryBag.transform.childCount)
                {
                    Transform slotTransform = inventoryBag.transform.GetChild(slotIndex);
                    Slot_State slotStateComponent = slotTransform.GetComponent<Slot_State>();
                    
                    if (slotStateComponent != null)
                    {
                        slotStateComponent.gameObject.SetActive(true);
                        
                        SlotState currentState = slots[x, y];
                        
                        // 슬롯 색상 설정
                        if (slotStateComponent.image != null)
                        {
                            Color slotColor = GetSlotColor(currentState, slotStateComponent);
                            
                            // Locked 슬롯은 완전 투명하게 설정 (A값 0)
                            if (currentState == SlotState.Locked)
                            {
                                slotColor.a = 0f; // 완전 투명
                            }
                            
                            slotStateComponent.image.color = slotColor;
                        }
                    }
                }
            }
        }
        
        // 활성화된 범위에 맞게 가방 크기 조절
        AdjustInventoryBagSizeForRange(minX, maxX, minY, maxY);
    }
    
    // 활성화된 범위에 맞게 가방 크기 조절
    void AdjustInventoryBagSizeForRange(int minX, int maxX, int minY, int maxY)
    {
        if (inventoryBag == null) return;
        
        // 활성화된 범위의 크기 계산
        int rangeWidth = maxX - minX + 1;
        int rangeHeight = maxY - minY + 1;
        
        // 가방 크기 설정 (범위+1)*100
        float newWidth = (rangeWidth + 1) * 100f;
        float newHeight = (rangeHeight + 1) * 100f;
        
        RectTransform bagRect = inventoryBag.GetComponent<RectTransform>();
        if (bagRect != null)
        {
            bagRect.sizeDelta = new Vector2(newWidth, newHeight);
        }
        
        Debug.Log($"슬롯 장착 후 가방 크기 조절: {rangeWidth}x{rangeHeight} -> {newWidth}x{newHeight}");
    }
    
    // Slot 아이템 드래그 종료 시 원상복구
    public void RestoreSlotActivationForSlotItem()
    {
        // 슬롯 활성화 상태 복원
        UpdateSlotActivation();
        
        // 가방 크기 재조절
        AdjustInventoryBagSize();
    }
    
    // 기존 아이템들의 위치 재조정
    public void RepositionExistingItems()
    {
        if (occupiedWeaponSlot == null) return;
        
        // Grid Layout Group 업데이트를 위한 지연 처리
        StartCoroutine(RepositionItemsAfterLayoutUpdate());
    }
    
    // Grid Layout Group 업데이트 후 아이템 위치 재조정
    System.Collections.IEnumerator RepositionItemsAfterLayoutUpdate()
    {
        // 한 프레임 대기 (Grid Layout Group이 슬롯들을 재배치할 시간 제공)
        yield return null;
        
        // occupiedWeaponSlot의 모든 자식 아이템들 찾기
        for (int i = 0; i < occupiedWeaponSlot.transform.childCount; i++)
        {
            Transform child = occupiedWeaponSlot.transform.GetChild(i);
            Item_System itemSystem = child.GetComponent<Item_System>();
            
            if (itemSystem != null && itemSystem.isPlacedInInventory)
            {
                // 아이템이 점유하고 있는 슬롯들의 새로운 위치 계산
                itemSystem.RepositionToSlots();
            }
        }
    }
    
    // 슬롯 색상 가져오기 (Slot_State의 색상 필드 사용)
    Color GetSlotColor(SlotState slotState, Slot_State slotStateComponent)
    {
        if (slotStateComponent == null) return new Color(1f, 1f, 1f, 1f); // 기본 흰색 (null 체크용)
        
        switch (slotState)
        {
            case SlotState.Empty:
                return slotStateComponent.defaultColor; // 기본 색상
            case SlotState.Locked:
                return slotStateComponent.lockedColor; // 잠금 색상
            case SlotState.Occupied:
            case SlotState.None:
            default:
                return slotStateComponent.defaultColor; // 기본색 그대로
        }
    }
    
    // OccupiedItemDatas에서 특정 아이템의 데이터 업데이트
    public void UpdateOccupiedItemData(Item_System itemSystem)
    {
        if (itemSystem == null || itemSystem.itemData == null || OccupiedItemDatas == null)
            return;
            
        // 해당 아이템과 일치하는 레코드 찾기 (itemData, name, level 기준)
        for (int i = 0; i < OccupiedItemDatas.Count; i++)
        {
            var record = OccupiedItemDatas[i];
            if (record != null && 
                record.itemData == itemSystem.itemData && 
                record.itemName == itemSystem.itemName &&
                record.itemLevel == itemSystem.itemLevel - 1) // 업그레이드 전 레벨과 일치
            {
                // 레코드 업데이트
                record.itemLevel = itemSystem.itemLevel;
                record.itemValue = itemSystem.itemValue;
                record.itemCooldown = itemSystem.itemCooldown;
                
                Debug.Log($"OccupiedItemDatas 업데이트: {itemSystem.itemName} 레벨 {record.itemLevel}로 업그레이드");
                break;
            }
        }
        // UI 업데이트
        inGameSystem.inGameUI.UpdateItemStatsUI();
    }
    
    // 장착한 아이템 번들 데이터 초기화
    public void ClearOccupiedItemDatas()
    {
        if (OccupiedItemDatas != null)
        {
            OccupiedItemDatas.Clear();
        }
        // UI 업데이트
        inGameSystem.inGameUI.UpdateItemStatsUI();
    }
    
    // 인벤토리 상태를 최초 상태로 되돌리기
    public void ResetInventoryToInitialState()
    {
        // 1. occupiedWeaponSlot의 모든 자식 아이템 제거
        if (occupiedWeaponSlot != null)
        {
            for (int i = occupiedWeaponSlot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = occupiedWeaponSlot.transform.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        // 2. inventoryBag의 슬롯에서 아이템 제거 (Occupied 상태인 슬롯의 자식 아이템들)
        if (inventoryBag != null)
        {
            for (int i = 0; i < inventoryBag.transform.childCount; i++)
            {
                Transform slotTransform = inventoryBag.transform.GetChild(i);
                Slot_State slotStateComponent = slotTransform.GetComponent<Slot_State>();
                
                if (slotStateComponent != null)
                {
                    // 슬롯의 자식 아이템들 제거
                    for (int j = slotTransform.childCount - 1; j >= 0; j--)
                    {
                        Transform child = slotTransform.GetChild(j);
                        if (child != null)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }
        }
        
        // 3. 장착한 아이템 번들 데이터 초기화
        ClearOccupiedItemDatas();
        
        // 4. 인벤토리 상태 초기화
        InitializeInventory();
        
        // 5. UI 업데이트 (ClearOccupiedItemDatas에서 이미 호출되지만 확실히 하기 위해)
        inGameSystem.inGameUI.UpdateItemStatsUI();
    }
}
