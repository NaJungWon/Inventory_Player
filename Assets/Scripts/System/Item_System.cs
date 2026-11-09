using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Item_System : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("아이템 데이터")]
    public Item_Data itemData;
    [Header("아이템 현재 레벨/데미지/쿨타임/종류")]
    public string itemName;
    public int itemLevel;
    public int itemValue;
    public float itemCooldown;
    public Item_Data.ItemType itemType;

    public AudioClip successSound;
    private AudioSource audioSource;
    
    [Header("테트리스식 인벤토리 설정")]
    public int slotWidth = 1;  // 아이템이 차지하는 가로 슬롯 수
    public int slotHeight = 1; // 아이템이 차지하는 세로 슬롯 수
    public Vector2Int[,] slotShape; // 아이템의 모양 (예: L자, T자 등)
    
    [Header("특수 모양 설정")]
    public ItemShapeType shapeType = ItemShapeType.Rectangle; // 아이템 모양 타입
    
    [System.Serializable]
    public class SlotData
    {
        public bool isUsed = true; // 해당 슬롯을 사용할지 여부
    }
    
    [Header("커스텀 모양 슬롯 리스트")]
    public List<SlotData> slotList = new List<SlotData>(); // 커스텀 모양의 각 슬롯 데이터
    
    public enum ItemShapeType
    {
        Rectangle,  // 사각형 (1x1, 2x2 등)
        Custom      // 커스텀 모양
    }
    
    [Header("드래그 앤 드롭")]
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;
    public Inventory_System inventorySystem;
    public GameObject reRollSlot;
    
    [Header("자석 효과 설정")]
    public float magnetDistance = 100f; // 자석 효과가 작동하는 거리
    public Color validPlacementColor = Color.green; // 유효한 배치 색상
    public Color invalidPlacementColor = Color.red; // 무효한 배치 색상
    
    // 아이템이 현재 차지하고 있는 슬롯 위치들
    private List<Vector2Int> occupiedSlots = new List<Vector2Int>();
    private List<Vector2Int> previewSlots = new List<Vector2Int>(); // 미리보기 슬롯들
    private Vector2Int currentSnapPosition = Vector2Int.one * -1; // 현재 스냅된 위치
    private bool isValidPlacement = false; // 현재 위치가 유효한지
    public bool isPlacedInInventory = false; // 인벤토리에 실제 장착 여부
    // 드래그 취소 시 복원을 위한 이전 점유 슬롯 기록
    private List<Vector2Int> previousOccupiedSlots = new List<Vector2Int>();
    private bool wasPlacedBeforeDrag = false;
    
    // 아이템 연출 관련
    private Coroutine itemAnimationCoroutine;

    void Start()
    {
        //itemType = itemData.itemType;
        //itemLevel = itemData.itemLevel;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        inventorySystem = FindObjectOfType<Inventory_System>();
        reRollSlot = inventorySystem.weaponSlot.gameObject;
        InitializeSlotShape();
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = successSound;
    }
    

    void Update()
    {
        
    }
    
    // 슬롯 모양 초기화
    void InitializeSlotShape()
    {
        switch (shapeType)
        {
            case ItemShapeType.Rectangle:
                InitializeRectangleShape();
                break;
            case ItemShapeType.Custom:
                InitializeCustomShape();
                break;
        }
    }
    
    // 사각형 모양 초기화
    void InitializeRectangleShape()
    {
        slotShape = new Vector2Int[slotWidth, slotHeight];
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                slotShape[x, y] = new Vector2Int(x, y);
            }
        }
    }
    
    // 커스텀 모양 초기화
    void InitializeCustomShape()
    {
        // slotWidth와 slotHeight에 맞게 slotList 초기화
        int totalSlots = slotWidth * slotHeight;
        
        // slotList가 비어있거나 크기가 맞지 않으면 초기화
        if (slotList == null || slotList.Count != totalSlots)
        {
            slotList = new List<SlotData>();
            for (int i = 0; i < totalSlots; i++)
            {
                slotList.Add(new SlotData { isUsed = true }); // 기본적으로 모든 슬롯 사용
            }
        }
        
        // slotShape 배열 초기화
        slotShape = new Vector2Int[slotWidth, slotHeight];
        
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                // slotList의 인덱스 계산 (가로 우선, 세로로 내려가는 순서)
                int slotIndex = y * slotWidth + x;
                
                if (slotIndex < slotList.Count && slotList[slotIndex].isUsed)
                {
                    slotShape[x, y] = new Vector2Int(x, y);
                }
                else
                {
                    slotShape[x, y] = new Vector2Int(-1, -1); // 비어있음
                }
            }
        }
    }
    
    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;
        originalParent = transform.parent;
        isDragging = true;
        
        // 드래그 시작 시 이전 점유 상태 기록 (복원용)
        wasPlacedBeforeDrag = isPlacedInInventory;
        previousOccupiedSlots = new List<Vector2Int>(occupiedSlots);
        
        // 현재 차지하고 있는 슬롯들을 비움
        ClearOccupiedSlots();

        // Slot 아이템인 경우 모든 슬롯 활성화 (순서 중요)
        if (itemType == Item_Data.ItemType.Slot && inventorySystem != null)
        {
            inventorySystem.ActivateAllSlotsForSlotItem();
            // 슬롯 아이템 드래그 시작 시 기존 아이템들 위치 재조정
            inventorySystem.RepositionExistingItems();
        }
        
        // 드래그 중에는 다른 UI와 상호작용하지 않도록 설정
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // 드래그 중에는 최상위로 이동
        transform.SetParent(transform.root);
    }
    
    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            // 드래그 중에는 마우스를 따라 자유롭게 이동
            transform.position = eventData.position;
            
            // 자석 효과: 가장 가까운 유효한 슬롯 위치 찾기 (미리보기용)
            Vector2Int snapPosition = FindNearestValidSlot(eventData.position);
            
            if (snapPosition != currentSnapPosition)
            {
                // 이전 미리보기 제거
                ClearPreview();
                
                // 새로운 위치로 스냅
                currentSnapPosition = snapPosition;
                
                if (currentSnapPosition.x >= 0 && currentSnapPosition.y >= 0)
                {
                    // 유효한 위치인지 확인
                    isValidPlacement = CanPlaceAtPosition(currentSnapPosition);
                    
                    // 미리보기만 표시 (실제 이동은 하지 않음)
                    ShowPreview(currentSnapPosition, isValidPlacement);
                }
            }
        }
    }
    
    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // 미리보기 정리
        ClearPreview();
        
        // Slot 아이템인 경우 슬롯 활성화 상태 즉시 복원
        if (itemType == Item_Data.ItemType.Slot && inventorySystem != null)
        {
            inventorySystem.RestoreSlotActivationForSlotItem();
            // 슬롯 아이템 드래그 종료 시 기존 아이템들 위치 재조정
            inventorySystem.RepositionExistingItems();
        }

        // 스냅된 위치가 유효한지 확인하고 순간이동
        if (currentSnapPosition.x >= 0 && currentSnapPosition.y >= 0 && isValidPlacement)
        {
            // 유효한 위치로 순간이동
            SnapToPosition(currentSnapPosition);
            
            // 슬롯들을 점유 상태로 변경
            OccupySlots(currentSnapPosition);
            PlaceItemAtPosition(currentSnapPosition);
            currentSnapPosition = Vector2Int.one * -1;
            return;
        }
        
        // 드롭 위치 확인 - 모든 장비 아이템들을 우선적으로 체크 (최우선 처리)
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        
        // 1. weaponSlot의 자식 아이템들 체크
        if (reRollSlot != null)
        {
            float minDistance = float.MaxValue;
            Item_System closestItem = null;
            
            // weaponSlot의 모든 자식 아이템들을 확인
            for (int i = 0; i < reRollSlot.transform.childCount; i++)
            {
                Transform child = reRollSlot.transform.GetChild(i);
                Item_System childItem = child.GetComponent<Item_System>();
                
                if (childItem != null && childItem != this) // 자기 자신 제외
                {
                    // 마우스 위치와 아이템의 거리 계산
                    float distance = Vector3.Distance(eventData.position, child.position);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestItem = childItem;
                    }
                }
            }
            
            // 가장 가까운 아이템과의 거리가 일정 범위 내에 있고 업그레이드 가능하면 합치기
            if (closestItem != null && minDistance < 50f && CanUpgrade(closestItem))
            {
                UpgradeItem(closestItem);
                return;
            }
        }
        
        // 2. occupiedWeaponSlot의 자식 아이템들 체크
        if (inventorySystem != null && inventorySystem.occupiedWeaponSlot != null)
        {
            float minDistance = float.MaxValue;
            Item_System closestItem = null;
            
            // occupiedWeaponSlot의 모든 자식 아이템들을 확인
            for (int i = 0; i < inventorySystem.occupiedWeaponSlot.transform.childCount; i++)
            {
                Transform child = inventorySystem.occupiedWeaponSlot.transform.GetChild(i);
                Item_System childItem = child.GetComponent<Item_System>();
                
                if (childItem != null && childItem != this) // 자기 자신 제외
                {
                    // 마우스 위치와 아이템의 거리 계산
                    float distance = Vector3.Distance(eventData.position, child.position);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestItem = childItem;
                    }
                }
            }
            
            // 가장 가까운 아이템과의 거리가 일정 범위 내에 있고 업그레이드 가능하면 합치기
            if (closestItem != null && minDistance < 50f && CanUpgrade(closestItem))
            {
                UpgradeItem(closestItem);
                return;
            }
        }
        
        // 일반적인 드롭 위치 확인
        if (dropTarget != null)
        {
            // 다른 아이템 위에 드롭된 경우 (업그레이드)
            Item_System targetItem = dropTarget.GetComponent<Item_System>();
            if (targetItem != null && CanUpgrade(targetItem))
            {
                UpgradeItem(targetItem);
                return;
            }
            
            // 인벤토리 슬롯에 드롭된 경우
            Slot_State slotState = dropTarget.GetComponent<Slot_State>();
            if (slotState != null)
            {
                TryPlaceInInventory(slotState);
                return;
            }
        }
        
        // weaponSlot 영역 체크 (아이템 합치기보다 나중에 처리)
        if (reRollSlot != null)
        {
            RectTransform rerollRect = reRollSlot.GetComponent<RectTransform>();
            RectTransform itemRect = GetComponent<RectTransform>();
            if (rerollRect != null && itemRect != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        rerollRect,
                        eventData.position,
                        eventData.pressEventCamera))
                {
                    // weaponSlot 자체가 아닌, weaponSlot의 자식 아이템 위에 드롭된 경우는 이미 위에서 처리됨
                    // weaponSlot의 빈 공간에 드롭된 경우만 여기서 처리
                    if (transform.parent != reRollSlot.transform)
                    {
                        // 기존 점유 슬롯 해제
                        ClearOccupiedSlots();
                        // 부모를 reRollSlot으로 변경하고 중앙 정렬
                        transform.SetParent(reRollSlot.transform, false);
                        itemRect.anchoredPosition = Vector2.zero;
                    }
                    return;
                }
            }
        }
        
        // 드롭 실패 시 원래 위치로 복귀
        RestorePreviousOccupationIfNeeded();
        ReturnToOriginalPosition();
    }
    
    // 인벤토리에 아이템 배치 시도
    void TryPlaceInInventory(Slot_State targetSlot)
    {
        if (inventorySystem == null) return;
        
        // 타겟 슬롯의 위치 계산
        Vector2Int targetPosition = GetSlotPosition(targetSlot);
        
        // 해당 위치에 아이템을 배치할 수 있는지 확인
        if (CanPlaceAtPosition(targetPosition))
        {
            // 슬롯들을 점유 상태로 변경
            OccupySlots(targetPosition);
            
            // 아이템을 해당 위치에 배치
            PlaceItemAtPosition(targetPosition);
        }
        else
        {
            // 배치 실패 시 이전 점유 상태를 복원하고 원래 위치로 복귀
            // (복원하지 않으면 가방 안 아이템이 제자리로 와도 칸이 빈 상태로 남음)
            RestorePreviousOccupationIfNeeded();
            ReturnToOriginalPosition();
        }
    }
    
    // 특정 위치에 아이템을 배치할 수 있는지 확인
    bool CanPlaceAtPosition(Vector2Int position)
    {
        if (inventorySystem == null) return false;
        
        // ItemType에 따라 다른 배치 조건 적용
        if (itemType == Item_Data.ItemType.Slot)
        {
            return CanPlaceSlotAtPosition(position);
        }
        else // Item_Data.ItemType.Item
        {
            return CanPlaceItemAtPosition(position);
        }
    }
    
    // 일반 아이템 배치 가능 여부 확인
    bool CanPlaceItemAtPosition(Vector2Int position)
    {
        // 슬롯 모양에 따라 각 위치 확인
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int shapeOffset = slotShape[x, y];
                
                // 비어있는 부분은 건너뛰기 (Vector2Int(-1,-1))
                if (shapeOffset.x == -1 && shapeOffset.y == -1)
                    continue;
                
                Vector2Int checkPos = position + shapeOffset;
                
                // 인벤토리 범위를 벗어나는지 확인
                if (checkPos.x < 0 || checkPos.x >= inventorySystem.inventoryMaxWidth ||
                    checkPos.y < 0 || checkPos.y >= inventorySystem.inventoryMaxHeight)
                {
                    return false;
                }
                
                // 해당 슬롯이 비어있지 않은지 확인
                if (inventorySystem.slots[checkPos.x, checkPos.y] != Inventory_System.SlotState.Empty)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // 슬롯 아이템 배치 가능 여부 확인 (Locked 슬롯에만 배치 가능)
    bool CanPlaceSlotAtPosition(Vector2Int position)
    {
        // 슬롯 모양에 따라 각 위치 확인
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int shapeOffset = slotShape[x, y];
                
                // 비어있는 부분은 건너뛰기 (Vector2Int(-1,-1))
                if (shapeOffset.x == -1 && shapeOffset.y == -1)
                    continue;
                
                Vector2Int checkPos = position + shapeOffset;
                
                // 인벤토리 범위를 벗어나는지 확인
                if (checkPos.x < 0 || checkPos.x >= inventorySystem.inventoryMaxWidth ||
                    checkPos.y < 0 || checkPos.y >= inventorySystem.inventoryMaxHeight)
                {
                    return false;
                }
                
                // 슬롯 아이템은 Locked 상태인 슬롯에만 배치 가능
                if (inventorySystem.slots[checkPos.x, checkPos.y] != Inventory_System.SlotState.Locked)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // 슬롯들을 점유 상태로 변경
    void OccupySlots(Vector2Int position)
    {
        occupiedSlots.Clear();
        
        // ItemType에 따라 다른 점유 로직 적용
        if (itemType == Item_Data.ItemType.Slot)
        {
            OccupySlotsForSlotItem(position);
        }
        else // Item_Data.ItemType.Item
        {
            OccupySlotsForItem(position);
        }
    }
    
    // 일반 아이템 슬롯 점유
    void OccupySlotsForItem(Vector2Int position)
    {
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int shapeOffset = slotShape[x, y];
                
                // 비어있는 부분은 건너뛰기 (Vector2Int(-1,-1))
                if (shapeOffset.x == -1 && shapeOffset.y == -1)
                    continue;
                
                Vector2Int slotPos = position + shapeOffset;
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Occupied;
                occupiedSlots.Add(slotPos);
                
                // 해당 슬롯의 Slot_State 컴포넌트 업데이트
                UpdateSlotState(slotPos, Slot_State.SlotState.Occupied);
            }
        }
        isPlacedInInventory = true;
    }
    
    // 슬롯 아이템 슬롯 점유 (Locked를 Empty로 변경)
    void OccupySlotsForSlotItem(Vector2Int position)
    {
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int shapeOffset = slotShape[x, y];
                
                // 비어있는 부분은 건너뛰기 (Vector2Int(-1,-1))
                if (shapeOffset.x == -1 && shapeOffset.y == -1)
                    continue;
                
                Vector2Int slotPos = position + shapeOffset;
                // 슬롯 아이템은 Locked를 Empty로 변경 (슬롯을 열어줌)
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Empty;
                occupiedSlots.Add(slotPos);
                
                // 해당 슬롯의 Slot_State 컴포넌트 업데이트
                UpdateSlotState(slotPos, Slot_State.SlotState.Empty);
            }
        }
        
        // 슬롯 아이템 성공적 장착 후 활성화된 범위 재계산 및 가방 크기 조절
        if (inventorySystem != null)
        {
            inventorySystem.UpdateInventoryAfterSlotPlacement();
            // 기존 아이템들의 위치 재조정
            inventorySystem.RepositionExistingItems();
        }

        audioSource.Play();
        // 슬롯 아이템 장착 후 삭제
        Destroy(gameObject);
    }
    
    // 현재 점유하고 있는 슬롯들을 비움
    void ClearOccupiedSlots()
    {
        // ItemType에 따라 다른 해제 로직 적용
        if (itemType == Item_Data.ItemType.Slot)
        {
            ClearOccupiedSlotsForSlotItem();
        }
        else // Item_Data.ItemType.Item
        {
            ClearOccupiedSlotsForItem();
        }
        occupiedSlots.Clear();
    }
    
    // 일반 아이템 슬롯 해제
    void ClearOccupiedSlotsForItem()
    {
        // 실제로 인벤토리를 점유했던 경우에만 장착 목록에서 제거
        if (isPlacedInInventory)
        {
            RemoveFromOccupiedIfNeeded();
        }
        foreach (Vector2Int slotPos in occupiedSlots)
        {
            if (inventorySystem != null)
            {
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Empty;
                
                // 해당 슬롯의 Slot_State 컴포넌트를 Empty로 복원
                UpdateSlotState(slotPos, Slot_State.SlotState.Empty);
            }
        }
        isPlacedInInventory = false;
    }
    
    // 슬롯 아이템 슬롯 해제 (Empty를 다시 Locked로 되돌림)
    void ClearOccupiedSlotsForSlotItem()
    {
        foreach (Vector2Int slotPos in occupiedSlots)
        {
            if (inventorySystem != null)
            {
                // 슬롯 아이템을 제거하면 다시 Locked로 되돌림
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Locked;
                
                // 해당 슬롯의 Slot_State 컴포넌트를 Locked로 복원
                UpdateSlotState(slotPos, Slot_State.SlotState.Locked);
            }
        }
        
        // 슬롯 아이템 제거 후 활성화된 범위 재계산 및 가방 크기 조절
        if (inventorySystem != null)
        {
            inventorySystem.UpdateInventoryAfterSlotPlacement();
        }
    }
    
    // 아이템을 특정 위치에 배치
    void PlaceItemAtPosition(Vector2Int position)
    {
        // 여기서 실제 위치 계산 및 배치 로직 구현
        // Grid Layout Group을 사용한다면 자동으로 정렬될 것
        // 인벤토리에 정상 배치되었으므로 무기(ItemType.Item)라면 OccupiedItemDatas에 등록
        AddToOccupiedIfNeeded();
        
        // 인벤토리에 성공적으로 배치된 아이템을 occupiedWeaponSlot에 자식으로 설정
        if (inventorySystem != null && inventorySystem.occupiedWeaponSlot != null)
        {
            // 현재 월드 좌표 저장
            Vector3 worldPosition = transform.position;
            // 부모 변경
            transform.SetParent(inventorySystem.occupiedWeaponSlot.transform, false);
            // 월드 좌표 복원
            transform.position = worldPosition;
        }
        // 인벤토리 장착 성공 시 파티클 실행 (Slot 아이템 제외)
        if (itemType == Item_Data.ItemType.Item)
        {
            PlayItemParticle(this);
            // 장착 연출 (EaseOutBack)
            PlayItemAnimation(false);
        }
        audioSource.Play();
    }
    
    // 슬롯 위치 계산 (Grid Layout Group 기준)
    Vector2Int GetSlotPosition(Slot_State slot)
    {
        // Grid Layout Group의 순서에 따라 위치 계산
        int index = slot.transform.GetSiblingIndex();
        return new Vector2Int(index % inventorySystem.inventoryMaxWidth, 
                             index / inventorySystem.inventoryMaxWidth);
    }
    
    // 업그레이드 가능한지 확인
    bool CanUpgrade(Item_System targetItem)
    {
        // 슬롯 아이템은 합쳐지지 않도록 처리
        if (this.itemType == Item_Data.ItemType.Slot || targetItem.itemType == Item_Data.ItemType.Slot)
        {
            return false;
        }
        
        // 레벨 아이콘 수가 최대 레벨 (5장 → 5레벨). 최대 레벨이면 합치지 않음
        return targetItem.itemData == this.itemData &&
               targetItem.itemLevel == this.itemLevel &&
               targetItem.itemLevel < targetItem.itemData.itemImages.Count;
    }
    
    // 아이템 업그레이드
    void UpgradeItem(Item_System targetItem)
    {
        // 레벨 증가
        targetItem.itemLevel++;
        targetItem.itemValue = (int)(targetItem.itemData.itemValue * targetItem.itemLevel);
        // 쿨타임은 레벨만큼 퍼센트로 감소 (레벨 1: 100%, 레벨 2: 90%, 레벨 3: 80% ...)
        float cooldownReduction = 1.0f - (0.1f * (targetItem.itemLevel - 1));
        targetItem.itemCooldown = targetItem.itemData.itemCooldown * cooldownReduction;
        targetItem.transform.GetChild(1).GetComponent<Image>().sprite = targetItem.itemData.itemImages[targetItem.itemLevel - 1];
        
        
        // 아이템 업그레이드 파티클 실행
        PlayItemParticle(targetItem);
        // 업그레이드 연출 (EaseOutBack)
        targetItem.PlayItemAnimation(true);
        // OccupiedItemDatas 업데이트 (인벤토리에 장착된 아이템인 경우)
        if (targetItem.isPlacedInInventory && inventorySystem != null)
        {
            inventorySystem.UpdateOccupiedItemData(targetItem);
        }
        
        targetItem.audioSource.Play();
        
        // 현재 아이템 제거
        Destroy(gameObject);
    }
    
    // 아이템 파티클 재생 (장착/업그레이드 공용)
    void PlayItemParticle(Item_System targetItem)
    {
        if (targetItem == null) return;
        if (targetItem.transform.childCount < 3) return;
        
        ParticleSystem particle = targetItem.transform.GetChild(2).GetComponent<ParticleSystem>();
        
        // 파티클을 찾았으면 활성화하고 재생
        if (particle != null)
        {
            // 파티클의 위치를 아이템의 위치로 설정 (UI Canvas 위에 있을 경우)
            Canvas canvas = targetItem.GetComponentInParent<Canvas>();            
            // 파티클 모듈 확인 및 재생
            var main = particle.main;
            main.playOnAwake = false; // 자동 재생 방지
            
            // UI 파티클의 경우 Renderer 설정 확인
            var renderer = particle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // UI Canvas 위에 있는 경우 Sorting Order를 높게 설정
                if (canvas != null)
                {
                    renderer.sortingOrder = 5; // UI 위에 표시되도록
                }
            }
            
            // 파티클이 정지 상태인지 확인하고 재생
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear();
            particle.Play();
        }
    }
    
    // 원래 위치로 복귀
    void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        transform.SetParent(originalParent);
    }
    
    // 슬롯 상태 업데이트 (Slot_State 컴포넌트와 텍스트 동기화)
    void UpdateSlotState(Vector2Int slotPos, Slot_State.SlotState newState)
    {
        if (inventorySystem == null || inventorySystem.inventoryBag == null)
            return;
        
        // 슬롯 인덱스 계산
        int slotIndex = slotPos.y * inventorySystem.inventoryMaxWidth + slotPos.x;
        
        if (slotIndex >= 0 && slotIndex < inventorySystem.inventoryBag.transform.childCount)
        {
            Transform slotTransform = inventorySystem.inventoryBag.transform.GetChild(slotIndex);
            Slot_State slotStateComponent = slotTransform.GetComponent<Slot_State>();
            
            if (slotStateComponent != null)
            {
                // 슬롯 상태 업데이트
                slotStateComponent.slotState = newState;
                
                // 슬롯 색상 업데이트
                if (slotStateComponent.image != null)
                {
                    Color slotColor = GetSlotColor(newState, slotStateComponent);
                    
                    // Locked 슬롯은 완전 투명하게 설정 (A값 0)
                    if (newState == Slot_State.SlotState.Locked)
                    {
                        slotColor.a = 0f; // 완전 투명
                    }
                    
                    slotStateComponent.image.color = slotColor;
                }
                
                // 텍스트 업데이트 (주석처리)
                /*
                if (slotStateComponent.text != null)
                {
                    switch (newState)
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

    // 드래그 실패 시 기존 점유 상태 복원
    void RestorePreviousOccupationIfNeeded()
    {
        if (!wasPlacedBeforeDrag || previousOccupiedSlots == null || previousOccupiedSlots.Count == 0 || inventorySystem == null)
            return;
        
        if (itemType == Item_Data.ItemType.Item)
        {
            // 무기 아이템: 이전 점유 슬롯을 Occupied로 복원
            foreach (Vector2Int slotPos in previousOccupiedSlots)
            {
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Occupied;
                UpdateSlotState(slotPos, Slot_State.SlotState.Occupied);
            }
            occupiedSlots = new List<Vector2Int>(previousOccupiedSlots);
            isPlacedInInventory = true;
            AddToOccupiedIfNeeded(); // 번들 데이터 복원
        }
        else if (itemType == Item_Data.ItemType.Slot)
        {
            // 슬롯 아이템: 이전 점유 슬롯을 Empty로 복원 (열어둔 상태 유지)
            foreach (Vector2Int slotPos in previousOccupiedSlots)
            {
                inventorySystem.slots[slotPos.x, slotPos.y] = Inventory_System.SlotState.Empty;
                UpdateSlotState(slotPos, Slot_State.SlotState.Empty);
            }
            occupiedSlots = new List<Vector2Int>(previousOccupiedSlots);
            isPlacedInInventory = true;
            if (inventorySystem != null)
            {
                inventorySystem.UpdateInventoryAfterSlotPlacement();
            }
        }
    }
    
    // 가장 가까운 유효한 슬롯 위치 찾기
    Vector2Int FindNearestValidSlot(Vector3 mousePosition)
    {
        if (inventorySystem == null || inventorySystem.inventoryBag == null)
            return Vector2Int.one * -1;
        
        float minDistance = float.MaxValue;
        Vector2Int nearestSlot = Vector2Int.one * -1;
        
        // 모든 슬롯을 확인하여 가장 가까운 위치 찾기
        for (int i = 0; i < inventorySystem.inventoryBag.transform.childCount; i++)
        {
            Transform slotTransform = inventorySystem.inventoryBag.transform.GetChild(i);
            float distance = Vector3.Distance(mousePosition, slotTransform.position);
            
            if (distance < magnetDistance && distance < minDistance)
            {
                Vector2Int slotPos = GetSlotPosition(slotTransform.GetComponent<Slot_State>());
                if (slotPos.x >= 0 && slotPos.y >= 0)
                {
                    minDistance = distance;
                    nearestSlot = slotPos;
                }
            }
        }
        
        return nearestSlot;
    }
    
    // 특정 위치로 아이템 스냅
    void SnapToPosition(Vector2Int position)
    {
        if (inventorySystem == null || inventorySystem.inventoryBag == null)
            return;
        
        // 아이템이 차지하는 모든 슬롯의 중심점 계산
        Vector3 centerPosition = CalculateItemCenterPosition(position);
        transform.position = centerPosition;
    }
    
    // 아이템이 차지하는 슬롯들의 중심점 계산
    Vector3 CalculateItemCenterPosition(Vector2Int topLeftPosition)
    {
        if (inventorySystem == null || inventorySystem.inventoryBag == null)
            return Vector3.zero;
        
        Vector3 totalPosition = Vector3.zero;
        int totalSlotCount = 0;
        
        // 아이템이 차지하는 모든 슬롯의 위치를 합산
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int slotPos = topLeftPosition + new Vector2Int(x, y);
                
                // 범위 체크
                if (slotPos.x >= 0 && slotPos.x < inventorySystem.inventoryMaxWidth &&
                    slotPos.y >= 0 && slotPos.y < inventorySystem.inventoryMaxHeight)
                {
                    // 해당 슬롯의 실제 위치 가져오기
                    int slotIndex = slotPos.y * inventorySystem.inventoryMaxWidth + slotPos.x;
                    if (slotIndex < inventorySystem.inventoryBag.transform.childCount)
                    {
                        Transform slotTransform = inventorySystem.inventoryBag.transform.GetChild(slotIndex);
                        totalPosition += slotTransform.position;
                        totalSlotCount++;
                    }
                }
            }
        }
        
        // 전체 슬롯들의 평균 위치 반환 (중심점)
        return totalSlotCount > 0 ? totalPosition / totalSlotCount : Vector3.zero;
    }
    
    // 미리보기 표시
    void ShowPreview(Vector2Int position, bool isValid)
    {
        if (inventorySystem == null || inventorySystem.inventoryBag == null)
            return;
        
        previewSlots.Clear();
        
        // 아이템이 차지할 슬롯들 계산
        for (int x = 0; x < slotWidth; x++)
        {
            for (int y = 0; y < slotHeight; y++)
            {
                Vector2Int shapeOffset = slotShape[x, y];
                
                // 비어있는 부분은 건너뛰기 (Vector2Int(-1,-1))
                if (shapeOffset.x == -1 && shapeOffset.y == -1)
                    continue;
                
                Vector2Int slotPos = position + shapeOffset;
                
                // 범위 체크
                if (slotPos.x >= 0 && slotPos.x < inventorySystem.inventoryMaxWidth &&
                    slotPos.y >= 0 && slotPos.y < inventorySystem.inventoryMaxHeight)
                {
                    // 슬롯 오브젝트 찾기
                    int slotIndex = slotPos.y * inventorySystem.inventoryMaxWidth + slotPos.x;
                    if (slotIndex < inventorySystem.inventoryBag.transform.childCount)
                    {
                        Transform slotTransform = inventorySystem.inventoryBag.transform.GetChild(slotIndex);
                        Slot_State slotState = slotTransform.GetComponent<Slot_State>();
                        
                        if (slotState != null && slotState.image != null)
                        {
                            // ItemType이 Item인 경우, Locked 슬롯은 미리보기 색상 변경하지 않음
                            bool isLockedSlot = (inventorySystem.slots[slotPos.x, slotPos.y] == Inventory_System.SlotState.Locked);
                            bool shouldSkipColorChange = (itemType == Item_Data.ItemType.Item && isLockedSlot);
                            
                            if (!shouldSkipColorChange)
                            {
                                // 미리보기 색상 적용 (canoccupiedColor / cannotoccupiedColor 사용)
                                slotState.image.color = isValid ? slotState.canoccupiedColor : slotState.cannotoccupiedColor;
                            }
                            
                            // previewSlots에 추가 (색상 변경 여부와 관계없이 추가하여 ClearPreview에서 복원 가능하도록)
                            previewSlots.Add(slotPos);
                            
                            // 미리보기 중에는 텍스트를 "Preview"로 표시 (주석처리)
                            /*
                            if (slotState.text != null)
                            {
                                slotState.text.text = isValid ? "Valid" : "Invalid";
                            }
                            */
                        }
                    }
                }
            }
        }
    }
    
    // 미리보기 제거
    void ClearPreview()
    {
        foreach (Vector2Int slotPos in previewSlots)
        {
            if (inventorySystem != null && inventorySystem.inventoryBag != null)
            {
                int slotIndex = slotPos.y * inventorySystem.inventoryMaxWidth + slotPos.x;
                if (slotIndex < inventorySystem.inventoryBag.transform.childCount)
                {
                    Transform slotTransform = inventorySystem.inventoryBag.transform.GetChild(slotIndex);
                    Slot_State slotState = slotTransform.GetComponent<Slot_State>();
                    
                    if (slotState != null && slotState.image != null)
                    {
                        // 원래 색상으로 복원 (슬롯 상태에 따라)
                        Color originalColor = GetSlotColor(slotState.slotState, slotState);
                        
                        // Locked 슬롯은 완전 투명하게 설정 (A값 0)
                        if (this.itemType == Item_Data.ItemType.Item && slotState.slotState == Slot_State.SlotState.Locked)
                        {
                            originalColor.a = 0f; // 완전 투명
                        }
                        
                        slotState.image.color = originalColor;
                        
                        // 텍스트도 원래 상태로 복원 (주석처리)
                        /*
                        if (slotState.text != null)
                        {
                            switch (slotState.slotState)
                            {
                                case Slot_State.SlotState.None:
                                    slotState.text.text = "None";
                                    break;
                                case Slot_State.SlotState.Empty:
                                    slotState.text.text = "Empty";
                                    break;
                                case Slot_State.SlotState.Locked:
                                    slotState.text.text = "Locked";
                                    break;
                                case Slot_State.SlotState.Occupied:
                                    slotState.text.text = "Occupied";
                                    break;
                            }
                        }
                        */
                    }
                }
            }
        }
        previewSlots.Clear();
    }

    // 무기 장착 데이터에 추가 (번들 저장, 중복 허용)
    void AddToOccupiedIfNeeded()
    {
        if (inventorySystem == null) return;
        if (itemType != Item_Data.ItemType.Item) return;
        if (itemData == null) return;
        if (inventorySystem.OccupiedItemDatas == null) return;
        var rec = new Inventory_System.EquippedItemRecord
        {
            itemData = itemData,
            itemName = itemName,
            itemLevel = itemLevel,
            itemValue = itemValue,
            itemCooldown = itemCooldown
        };
        inventorySystem.OccupiedItemDatas.Add(rec);
        // UI 업데이트
        inventorySystem.inGameSystem.inGameUI.UpdateItemStatsUI();
    }

    // 무기 장착 데이터에서 제거 (동일 항목 1개 제거)
    void RemoveFromOccupiedIfNeeded()
    {
        if (inventorySystem == null) return;
        if (itemType != Item_Data.ItemType.Item) return;
        if (itemData == null) return;
        if (inventorySystem.OccupiedItemDatas == null) return;
        // 가장 먼저 매칭되는 번들 1개 제거 (itemData, name, level, value, cooldown 기준)
        for (int i = 0; i < inventorySystem.OccupiedItemDatas.Count; i++)
        {
            var rec = inventorySystem.OccupiedItemDatas[i];
            if (rec != null &&
                rec.itemData == itemData &&
                rec.itemName == itemName &&
                rec.itemLevel == itemLevel &&
                rec.itemValue == itemValue &&
                Mathf.Approximately(rec.itemCooldown, itemCooldown))
            {
                inventorySystem.OccupiedItemDatas.RemoveAt(i);
                break;
            }
        }
        // UI 업데이트
        inventorySystem.inGameSystem.inGameUI.UpdateItemStatsUI();
    }
    
    // 슬롯 위치로 아이템 재배치
    public void RepositionToSlots()
    {
        // 인벤토리에 장착된 아이템만 위치 재조정
        if (isPlacedInInventory && occupiedSlots.Count > 0)
        {
            // 현재 점유하고 있는 슬롯들의 중심점으로 위치 재조정
            Vector3 oldPosition = transform.position;
            Vector3 newPosition = CalculateItemCenterPosition(occupiedSlots[0]);
            transform.position = newPosition;
            
            // 디버깅용 로그
            Debug.Log($"아이템 위치 재조정: {itemName} - 이전: {oldPosition}, 새로운: {newPosition}, 점유 슬롯: {occupiedSlots[0]}");
        }
    }
    
    // 슬롯 색상 가져오기 (Slot_State의 색상 필드 사용)
    Color GetSlotColor(Slot_State.SlotState slotState, Slot_State slotStateComponent)
    {
        if (slotStateComponent == null) return new Color(1f, 1f, 1f, 1f); // 기본 흰색 (null 체크용)
        
        switch (slotState)
        {
            case Slot_State.SlotState.Empty:
                return slotStateComponent.defaultColor; // 기본 색상
            case Slot_State.SlotState.Locked:
                return slotStateComponent.lockedColor; // 잠금 색상
            case Slot_State.SlotState.Occupied:
            case Slot_State.SlotState.None:
            default:
                return slotStateComponent.defaultColor; // 기본색 그대로
        }
    }
    
    // 아이템 연출 애니메이션 (장착/업그레이드)
    void PlayItemAnimation(bool isUpgrade)
    {
        if (itemAnimationCoroutine != null)
        {
            StopCoroutine(itemAnimationCoroutine);
        }
        itemAnimationCoroutine = StartCoroutine(ItemAnimationCoroutine(isUpgrade));
    }
    
    private IEnumerator ItemAnimationCoroutine(bool isUpgrade)
    {
        float duration = isUpgrade ? 0.5f : 0.4f; // 업그레이드는 조금 더 길게
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 targetScale = Vector3.one;
        Vector3 peakScale = Vector3.one * (isUpgrade ? 1.3f : 1.15f); // 업그레이드는 더 크게
        
        // 시작: 작은 크기에서 시작
        transform.localScale = startScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 장착/업그레이드 모두 EaseOutBack 사용
            float eased = EaseOutBack(t);
            
            // 피크를 지나면서 정상 크기로 복귀
            if (t < 0.6f)
            {
                // 피크까지 확대
                float peakT = t / 0.6f;
                transform.localScale = Vector3.Lerp(startScale, peakScale, eased);
            }
            else
            {
                // 피크에서 정상 크기로 복귀
                float returnT = (t - 0.6f) / 0.4f;
                float returnEased = EaseOutBack(returnT);
                transform.localScale = Vector3.Lerp(peakScale, targetScale, returnEased);
            }
            
            yield return null;
        }
        
        transform.localScale = targetScale;
        itemAnimationCoroutine = null;
    }
    
    // EaseOutBack 함수
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
