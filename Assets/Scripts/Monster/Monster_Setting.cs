using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Monster_Setting : MonoBehaviour
{
    public GameObject player;
    public NavMeshAgent agent;
    public Unit_Status unitStatus;
    
    [Header("공격 설정")]
    public float attackCooldown = 1f; // 공격 쿨타임 (초)
    private float lastAttackTime = 0f; // 마지막 공격 시간
    public bool isAttacking = false; // 공격 중인지 여부
    public bool isHit = false; // 피격 중인지 여부
    // Start is called before the first frame update
    public float originSpeed;
    void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // 2D에서는 회전 불필요
        agent.updateUpAxis = false;   // NavMeshPlus에서 XY 평면 사용
        unitStatus = gameObject.GetComponent<Unit_Status>();
        originSpeed = agent.speed;
        attackCooldown = 1f / agent.speed; // agent.speed에 반비례 (speed=1 → 1초, speed=2 → 0.5초)
    }

    // Update is called once per frame
    void Update()
    {
        if(unitStatus.inGameSystem.inGameState == InGame_System.InGameState.Fight && unitStatus.isAlive)
        {
            if(agent.isStopped && !isHit)
            {
                agent.isStopped = false;
            }
            
            Vector3 target = player.transform.position;
            agent.SetDestination(target);
            
            // 이동 방향에 따라 rotation.y 설정 (좌우 방향만 체크)
            UpdateRotation();
            
            // agent가 목표 지점에 도달했는지 확인 (stoppingDistance 내에 있는지)
            // pathPending이 false이고 remainingDistance가 stoppingDistance 이하이면 도달한 것으로 판단
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // 공격 범위에 도달했으므로 IDLE 상태로 전환
                // 공격 쿨타임이 지났고 공격 중이 아니면 공격 실행
                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                {
                    // 공격 실행 (코루틴으로 ATTACK 후 IDLE로 전환)
                    StartCoroutine(ExecuteAttack());
                }
                else if (!isAttacking)
                {
                    // IDLE 애니메이션은 기본 속도로 재생
                    unitStatus.spumPrefabs._anim.speed = 1.0f;
                    // 공격 쿨타임 중이면 IDLE 애니메이션 유지
                    unitStatus.spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
                }
            }
            else
            {
                // 이동 중이면 MOVE 애니메이션 지속적으로 재생
                unitStatus.spumPrefabs.PlayAnimation(PlayerState.MOVE, 0);
                // MOVE 애니메이션 재생 속도를 agent.speed와 동일하게 설정
                unitStatus.spumPrefabs._anim.speed = originSpeed;
            }
        }
        else
        {
            agent.isStopped = true;
        }
    }
    
    // 이동 방향에 따라 rotation.y 설정 (좌우 방향만 체크)
    void UpdateRotation()
    {
        // agent의 velocity를 사용하여 이동 방향 확인
        Vector3 velocity = agent.velocity;
        
        // velocity가 거의 0이면 목표 위치와의 차이로 방향 판단
        if (velocity.magnitude < 0.1f && player != null)
        {
            Vector3 direction = (player.transform.position - transform.position).normalized;
            velocity = direction;
        }
        
        // x축 방향만 체크 (좌우 방향)
        if (velocity.x > 0.01f) // 오른쪽 이동
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (velocity.x < -0.01f) // 왼쪽 이동
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    // 공격 실행 코루틴 (ATTACK 애니메이션 재생 후 IDLE로 전환)
    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        // 애니메이션 속도를 기본 속도로 설정
        unitStatus.spumPrefabs._anim.speed = originSpeed;
        // Attack 애니메이션 실행
        unitStatus.spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);
        
        // 데미지 주기
        if (player != null)
        {
            player.GetComponent<Unit_Status>().UnitTakeDamage(unitStatus.unitDamage);
        }
        
        lastAttackTime = Time.time;
        
        // ATTACK 애니메이션 재생 시간 대기 (예: 0.5초)
        yield return new WaitForSeconds(0.5f);
        
        // IDLE 애니메이션으로 전환
        unitStatus.spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
        
        isAttacking = false;
    }
}
