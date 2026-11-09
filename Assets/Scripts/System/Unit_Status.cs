using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit_Status : MonoBehaviour
{
    public InGame_System inGameSystem;
    public Slider hpSlider;
    public int maxHp;
    public int unitHp;
    public int unitDamage;
    public bool isAlive = true;
    public SPUM_Prefabs spumPrefabs;
    public TextMeshProUGUI unitText; // 플레이어-체력 텍스트 / 몬스터 - 데미지 텍스트 표시
    private Monster_Setting monsterSetting;
    private Coroutine hitEffectCoroutine;
    private List<SpriteRenderer> cachedRenderers = new List<SpriteRenderer>(); // 스프라이트 렌더러 캐시
    private List<Material> cachedOriginalMaterials = new List<Material>(); // 원래 머티리얼 캐시
    public Material flashWhiteMat; // 플래시 흰색 머티리얼

    // Start is called before the first frame update
    void Start()
    {
        if(this.gameObject.tag == "Monster")
        {
            monsterSetting = GetComponent<Monster_Setting>();
        }
        
        if (transform.childCount > 0)
        {
            spumPrefabs = transform.GetChild(0).GetComponent<SPUM_Prefabs>();
            // OverrideControllerInit 호출하여 StateAnimationPairs 초기화
            if (spumPrefabs != null)
            {
                spumPrefabs.OverrideControllerInit();
                CacheSpriteRenderers();
            }
        }
    }

    public void SetHpSlider()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
            unitText.text = maxHp.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetUnitStatus()
    {
        unitHp = maxHp;
        isAlive = true;
        SetHpSlider();
        spumPrefabs._anim.SetBool("isDeath", false);
        spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
    }

    public void UnitTakeDamage(int damage)
    {
        if(!isAlive) return;
        
        unitHp -= damage;
        UnitHitEffect();
        // unitHp가 변경될 때마다 slider 업데이트
        UpdateHpSlider();
        
        if (unitHp <= 0)
        {   
            unitHp = 0;
            UpdateHpSlider();
            if(this.gameObject.tag == "Monster")
            {
                StopCoroutine(HitAction());
            }
            // Death 애니메이션 재생 후 처리 (코루틴)
            StartCoroutine(HandleDeath());
        }
        else if(this.gameObject.tag == "Monster")
        {
            StartCoroutine(HitAction());
        }
    }
    
    // HP Slider 업데이트
    public void UpdateHpSlider()
    {
        if (hpSlider != null)
        {
            // unitHp를 minValue와 maxValue 사이로 제한
            hpSlider.value = Mathf.Clamp(unitHp, hpSlider.minValue, hpSlider.maxValue);
            if(unitHp <= 0)
            {
                unitText.text = "0";
            }
            else
            {
                unitText.text = unitHp.ToString();
            }
        }
    }

    // 피격 이펙트 재생 코루틴
    void UnitHitEffect()
    {
        if (spumPrefabs == null || cachedRenderers.Count == 0 || flashWhiteMat == null) return;
        
        // 동시에 여러 번 실행되지 않도록 기존 코루틴 중지
        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
        }

        hitEffectCoroutine = StartCoroutine(HitEffectRoutine());
    }

    // 피격 이펙트 재생 코루틴
    IEnumerator HitEffectRoutine()
    {
        var mpb = new MaterialPropertyBlock();

        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            var renderer = cachedRenderers[i];
            if (renderer == null || flashWhiteMat == null) continue;

            // 플래시용 머티리얼 적용
            renderer.material = flashWhiteMat;

            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_FlashAmount", 1f);
            renderer.SetPropertyBlock(mpb);
        }

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            for (int i = 0; i < cachedRenderers.Count; i++)
            {
                var renderer = cachedRenderers[i];
                if (renderer == null || flashWhiteMat == null) continue;

                renderer.GetPropertyBlock(mpb);
                // 1 -> 0 으로 서서히 감소
                mpb.SetFloat("_FlashAmount", 1f - t);
                renderer.SetPropertyBlock(mpb);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            var renderer = cachedRenderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_FlashAmount", 0f);
            renderer.SetPropertyBlock(mpb);

            // 원래 머티리얼 복원
            if (i < cachedOriginalMaterials.Count && cachedOriginalMaterials[i] != null)
            {
                renderer.material = cachedOriginalMaterials[i];
            }
        }

        hitEffectCoroutine = null;
    }

    // 스프라이트 렌더러 캐시
    void CacheSpriteRenderers()
    {
        cachedRenderers.Clear();
        cachedOriginalMaterials.Clear();

        var renderers = spumPrefabs.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            cachedRenderers.Add(renderer);
            cachedOriginalMaterials.Add(renderer.sharedMaterial);
        }
    }

    // 몬스터 데미지 받을 때 애니메이션 재생 코루틴
    IEnumerator HitAction()
    {
        if(monsterSetting.isAttacking)
        {
            yield break;
        }
        else
        {
            monsterSetting.isHit = true;
            monsterSetting.agent.isStopped = true;
            //spumPrefabs._anim.speed = 1f;
            spumPrefabs.PlayAnimation(PlayerState.DAMAGED, 0);
            yield return new WaitForSeconds(0.3f);
            monsterSetting.isHit = false;
            monsterSetting.agent.isStopped = false;
            spumPrefabs.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    // Death 애니메이션 재생 후 처리하는 코루틴
    IEnumerator HandleDeath()
    {
        isAlive = false;
        
        // Death 애니메이션 재생
        if (spumPrefabs != null && spumPrefabs._anim != null)
        {
            spumPrefabs._anim.SetBool("isDeath", true);
            spumPrefabs.PlayAnimation(PlayerState.DEATH, 0);
        }
        
        // 0.5초 대기 (애니메이션 재생 시간 확보)
        yield return new WaitForSeconds(0.5f);
        
        // 태그에 따라 처리
        if(this.gameObject.tag == "Monster")
        {
            Destroy(gameObject);
        }
        else if(this.gameObject.tag == "Player")
        {
            Debug.Log("Player is dead");
            inGameSystem.inGameUI.GameOver();
        }
    }
}
