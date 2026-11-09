using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item_Function : MonoBehaviour
{
    public Inventory_System.EquippedItemRecord equippedItemRecord;
    public InGame_System inGameSystem;
    public GameObject player;
    public Item_Data.WeaponType weaponType;
    private float attackDelay = 0.2f;
    [Header("투사체 프리팹")]
    public GameObject projectile;
    [Header("수치값(데미지/힐량)")]
    public int itemValue;

    private float itemTimer;
    private GameObject attackRange;
    //public float itemCooldown;
    // Start is called before the first frame update
    void Start()
    {
        itemTimer = 0;
        // player가 설정되지 않았을 수 있으므로 Update에서 초기화
        Debug.Log($"아이템 정보: {equippedItemRecord.itemName}, 레벨: {equippedItemRecord.itemLevel}, 값: {equippedItemRecord.itemValue}, 쿨타임: {equippedItemRecord.itemCooldown}");
        Item_UI_Setting();
        projectile = equippedItemRecord.itemData.battleItem;
        itemValue = (int)(equippedItemRecord.itemData.itemValue * equippedItemRecord.itemLevel);
    }

    // Update is called once per frame
    void Update()
    {
        // player가 설정되었고 attackRange가 아직 설정되지 않았다면 초기화
        if (player != null && attackRange == null)
        {
            // 플레이어 오브젝트에 자식이 있는지 확인
            if (player.transform.childCount > 0)
            {
                attackRange = player.transform.GetChild(1).gameObject;
                Debug.Log($"AttackRange 설정 완료: {attackRange.name}");
            }
            else
            {
                Debug.LogWarning("플레이어 오브젝트에 자식이 없습니다.");
            }
        }
        
        if(inGameSystem.inGameState == InGame_System.InGameState.Fight)
        {
            itemTimer += Time.deltaTime;
        }
        
        // 쿨타임 UI 업데이트 (1에서 0으로 감소)
        Cooldown_UI_Setting();
        
        if (itemTimer >= equippedItemRecord.itemCooldown)
        {
            itemTimer = 0;
            
            // attackRange 내에 적이 있는지 확인
            GameObject target = FindTarget();
            
            if (target != null && player.GetComponent<Unit_Status>().isAlive)
            {
                player.GetComponent<Unit_Status>().spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);
                player.GetComponent<AudioSource>().Play();
                // 코루틴으로 공격 실행
                StartCoroutine(ExecuteAttack(target));
            }
            else
            {
                Debug.Log("공격 범위 내에 적이 없어서 공격하지 않습니다.");
            }
        }
    }

    void Item_UI_Setting()
    {
        this.gameObject.transform.GetChild(1).GetComponent<Image>().sprite = equippedItemRecord.itemData.itemImages[equippedItemRecord.itemLevel - 1];
    }

    void Cooldown_UI_Setting()
    {
        // fillAmount를 1에서 0으로 감소 (1 - (현재시간 / 전체쿨타임))
        float fillAmount = 1f - (itemTimer / equippedItemRecord.itemCooldown);
        this.gameObject.transform.GetChild(2).GetComponent<Image>().fillAmount = fillAmount;
    }

    void ThrowWeapon(GameObject target)
    {
        if (projectile == null || player == null)
        {
            Debug.LogWarning("ThrowWeapon: 필요한 컴포넌트가 설정되지 않았습니다.");
            return;
        }
        
        // 플레이어 위치에서 투사체 생성
        GameObject thrownProjectile = Instantiate(projectile, player.transform.position, Quaternion.identity);
        // 투사체 이미지 설정
        thrownProjectile.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = this.gameObject.transform.GetChild(1).GetComponent<Image>().sprite;
        // Weapon_Input_Damage 컴포넌트 확인 및 설정
        Weapon_Input_Damage weaponDamageComponent = thrownProjectile.GetComponent<Weapon_Input_Damage>();
        if (weaponDamageComponent != null)
        {
            weaponDamageComponent.weaponDamage = itemValue;
            weaponDamageComponent.target = target;
        }
        else
        {
            Debug.LogError("투사체 프리팹에 Weapon_Input_Damage 컴포넌트가 없습니다!");
        }
        
        //Debug.Log($"투사체를 {target.name}에게 던졌습니다.");
    }

    void ShootWeapon()
    {
        itemTimer = 0;
    }

    void HealWeapon(GameObject player)
    {
        if (projectile == null || player == null)
        {
            Debug.LogWarning("ThrowWeapon: 필요한 컴포넌트가 설정되지 않았습니다.");
            return;
        }
        
        // 플레이어 위치에서 투사체 생성
        GameObject healProjectile = Instantiate(projectile, player.transform.position, Quaternion.identity);
        // 투사체 이미지 설정
        healProjectile.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = this.gameObject.transform.GetChild(1).GetComponent<Image>().sprite;
        Item_heal itemHealComponent = healProjectile.GetComponent<Item_heal>();
        if (itemHealComponent != null)
        {
            itemHealComponent.healValue = itemValue;
            itemHealComponent.player = player;
        }
        else
        {
            Debug.LogError("투사체 프리팹에 Item_heal 컴포넌트가 없습니다!");
        }

        //itemTimer = 0;
    }
    
    // 공격 실행 코루틴
    IEnumerator ExecuteAttack(GameObject target)
    {
        yield return new WaitForSeconds(attackDelay);
        
        if (weaponType == Item_Data.WeaponType.Throw)
        {
            ThrowWeapon(target);
        }
        else if (weaponType == Item_Data.WeaponType.Shoot)
        {
            ShootWeapon();
        }
        else if (weaponType == Item_Data.WeaponType.Heal)
        {
            HealWeapon(player);
        }
        //player.GetComponent<Unit_Status>().spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
    }
    
    // attackRange 내의 가장 가까운 적 찾기
    GameObject FindTarget()
    {
        if (attackRange == null) return null;
        
        // attackRange의 Collider2D 컴포넌트 가져오기
        Collider2D rangeCollider = attackRange.GetComponent<Collider2D>();
        if (rangeCollider == null) return null;
        
        // attackRange 내의 모든 몬스터 찾기
        Collider2D[] monstersInRange = new Collider2D[10]; // 최대 10개 몬스터
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Default")); // 기본 레이어
        filter.useTriggers = true;

        int monsterCount = rangeCollider.OverlapCollider(filter, monstersInRange);
        
        GameObject nearestMonster = null;
        float nearestDistance = float.MaxValue;
        
        for (int i = 0; i < monsterCount; i++)
        {
            GameObject monster = monstersInRange[i].gameObject;
            
            // 몬스터 태그 확인
            if (monster.CompareTag("Monster"))
            {
                // Unit_Status 컴포넌트 가져오기
                Unit_Status monsterStatus = monster.GetComponent<Unit_Status>();
                
                // isAlive가 true인 몬스터만 타겟으로 선택
                if (monsterStatus != null && monsterStatus.isAlive)
                {
                    float distance = Vector3.Distance(player.transform.position, monster.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestMonster = monster;
                    }
                }
            }
        }
        
        return nearestMonster;
    }
}
