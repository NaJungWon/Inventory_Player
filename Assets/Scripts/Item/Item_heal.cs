using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_heal : MonoBehaviour
{
    public int healValue;
    
    private Vector3 startPosition; // 투사체 초기 위치
    private Vector3 targetPosition; // 투사체 타겟 위치
    public Vector3 endPosition; // 투사체 최종 위치
    private float animationTime = 1.0f; // 애니메이션 총 시간
    private float currentTime = 0f;
    private bool isAnimating = false;
    private bool hasHealed = false; // 힐링이 실행되었는지 확인
    private bool hasPlayedParticle = false; // 파티클이 재생되었는지 확인
    public GameObject player;
    public ParticleSystem particle;
    
    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * 2f; // y좌표 2만큼 위로
        isAnimating = true;
        particle = this.gameObject.transform.GetChild(1).GetChild(0).GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        // 실시간으로 플레이어 좌표 업데이트
        if (player != null)
        {
            endPosition = player.transform.position;
        }
        
        if (isAnimating)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / animationTime;
            
            if (progress <= 0.5f)
            {
                // 위로 올라가는 애니메이션 (0 ~ 0.5)
                float upProgress = progress * 2f; // 0 ~ 1
                transform.position = Vector3.Lerp(startPosition, targetPosition, upProgress);
            }
            else
            {
                // 아래로 내려오는 애니메이션 (0.5 ~ 1) - endPosition으로
                float downProgress = (progress - 0.5f) * 2f; // 0 ~ 1
                transform.position = Vector3.Lerp(targetPosition, endPosition, downProgress);
                
                // 애니메이션 완료 시 힐링 실행 (한 번만)
                if (progress >= 1f && !hasHealed)
                {
                    isAnimating = false;
                    hasHealed = true;
                    HealPlayer();
                    
                    // 파티클을 찾았으면 활성화하고 재생 (한 번만)
                    if (particle != null && !hasPlayedParticle)
                    {
                        hasPlayedParticle = true;
                        
                        // 파티클의 위치를 아이템의 위치로 설정 (UI Canvas 위에 있을 경우)
                        Canvas canvas = this.gameObject.GetComponentInParent<Canvas>();            
                        // 파티클 모듈 확인 및 재생
                        var main = particle.main;
                        main.playOnAwake = false; // 자동 재생 방지
                        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        particle.Clear();
                        particle.Play();
                        // 파티클 재생 완료 후 파괴
                        StartCoroutine(DestroyAfterParticle());
                    }
                    else if (particle == null)
                    {
                        // 파티클이 없으면 즉시 파괴
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
    
    void HealPlayer()
    {
        if (player == null) return;
        
        // Unit_Status 컴포넌트 찾기
        Unit_Status unitStatus = player.GetComponent<Unit_Status>();
        if (unitStatus == null)
        {
            Debug.LogWarning("플레이어에 Unit_Status 컴포넌트가 없습니다!");
            return;
        }
        
        // HP 회복 (maxHp 제한)
        unitStatus.unitHp += healValue;
        if(unitStatus.unitHp > unitStatus.maxHp)
        {
            unitStatus.unitHp = unitStatus.maxHp;
        }
        unitStatus.UpdateHpSlider();
        
        Debug.Log($"플레이어 HP 회복: {healValue}만큼 회복, 현재 HP: {unitStatus.unitHp}");
    }
    
    // 파티클 재생 완료 후 오브젝트 파괴
    IEnumerator DestroyAfterParticle()
    {
        if (particle == null)
        {
            Destroy(gameObject);
            yield break;
        }
        
        // 파티클의 duration과 startLifetime을 확인하여 총 재생 시간 계산
        var main = particle.main;
        float duration = main.duration;
        float startLifetime = main.startLifetime.constant;
        
        // 파티클이 완전히 끝날 때까지 대기 (duration + startLifetime)
        float totalTime = duration + startLifetime;
        
        // 파티클이 재생 중이거나 파티클이 남아있으면 대기
        while (particle.isPlaying || particle.particleCount > 0)
        {
            yield return null;
        }
        
        // 추가로 안전하게 약간 더 대기 (모든 파티클이 완전히 사라질 때까지)
        yield return new WaitForSeconds(0.1f);
        
        // 오브젝트 파괴
        Destroy(gameObject);
    }
}
