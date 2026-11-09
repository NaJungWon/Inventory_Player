using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Input_Damage : MonoBehaviour
{
    public int weaponDamage;
    public float weaponSpeed; // 투사체 속도
    public GameObject target; // 추적할 타겟
    
    // Start is called before the first frame update
    void Start()
    {
        weaponSpeed = 5f;
    }

    // Update is called once per frame
    void Update()
    {
        // 타겟이 사라졌으면 투사체 파괴
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // 타겟 방향으로 실시간 이동
        Vector3 direction = (target.transform.position - transform.position).normalized;
        GetComponent<Rigidbody2D>().velocity = direction * weaponSpeed; // 속도 조정 가능
        
        // 투사체가 타겟 방향을 바라보도록 회전 (2D에서 정면 방향)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // -90도 오프셋으로 정면 방향
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터와 충돌했을 때
        if (other.CompareTag("Monster"))
        {
            // 충돌한 몬스터가 타겟과 같은 오브젝트인지 확인
            if (other.gameObject == target)
            {
                // 몬스터의 Unit_Status 컴포넌트에 데미지 적용
                Unit_Status monsterStatus = other.GetComponent<Unit_Status>();
                if (monsterStatus != null)
                {
                    monsterStatus.UnitTakeDamage(weaponDamage);
                    Debug.Log($"투사체가 타겟 몬스터에게 {weaponDamage} 데미지를 입혔습니다.");
                }
                
                // 투사체 파괴
                Destroy(gameObject);
            }
        }
    }
}
