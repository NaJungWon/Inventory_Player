using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Setting_Manager : MonoBehaviour
{
    public static Setting_Manager Instance { get; private set; }

    public Slider BGMslider; //배경음조절 슬라이더
    private float BGMvolume = 1; //이전 BGM볼륨값
    public Slider SFXslider; //효과음조절 슬라이더
    private float SFXvolume = 1; //이전 SFX볼륨값

    [Header("배경 사운드 설정")]
    public List<AudioSource> bgmSoundSource = new List<AudioSource>(); //배경음 사운드 오브젝트의 컴포넌트

    [Header("SFX 사운드 설정")]
    public List<AudioSource> sfxSoundSource = new List<AudioSource>(); //SFX 사운드 오브젝트의 컴포넌트

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 슬라이더 범위를 먼저 설정 (Inspector 값보다 우선)
        if (BGMslider != null)
        {
            BGMslider.maxValue = 1f;
            BGMslider.minValue = 0f;
        }
        if (SFXslider != null)
        {
            SFXslider.maxValue = 1f;
            SFXslider.minValue = 0f;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        // 슬라이더 범위 설정 (0~1) 및 초기값 1로 설정
        if (BGMslider != null)
        {
            //BGMslider.maxValue = 1;
            //BGMslider.minValue = 0f;
            //BGMslider.value = 1; // 초기값 1로 설정
            if(Game_Manager.Instance.isMusicVolumeValueSet)
            {
                BGMslider.value = Game_Manager.Instance.musicVolumeValue;
            }
            else
            {
                BGMslider.value = 1;
            }
            BGMvolume = BGMslider.value;
            // 슬라이더 값 변경 이벤트 리스너 추가
            BGMslider.onValueChanged.AddListener(OnBGMVolumeChanged);
            // 초기 볼륨 적용
            OnBGMVolumeChanged(BGMslider.value);
        }
        if (SFXslider != null)
        {
            //SFXslider.maxValue = 1;
            //SFXslider.minValue = 0f;
            //SFXslider.value = 1; // 초기값 1로 설정
            if(Game_Manager.Instance.isSfxVolumeValueSet)
            {
                SFXslider.value = Game_Manager.Instance.sfxVolumeValue;
            }
            else
            {
                SFXslider.value = 1;
            }
            SFXvolume = SFXslider.value;
            // 슬라이더 값 변경 이벤트 리스너 추가
            SFXslider.onValueChanged.AddListener(OnSFXVolumeChanged);
            // 초기 볼륨 적용
            OnSFXVolumeChanged(SFXslider.value);
        }
    }

    void Update()
    {
        // BGM 슬라이더 값 변경 감지 및 볼륨 업데이트
        if (BGMslider != null)
        {
            float currentBGMValue = BGMslider.value;
            if (currentBGMValue != BGMvolume)
            {
                // 슬라이더 값이 변경되었으므로 모든 BGM AudioSource 볼륨 업데이트
                OnBGMVolumeChanged(currentBGMValue);
                BGMvolume = currentBGMValue; // 이전 값 업데이트
                Game_Manager.Instance.musicVolumeValue = BGMvolume;
                if(!Game_Manager.Instance.isMusicVolumeValueSet)
                {
                    Game_Manager.Instance.isMusicVolumeValueSet = true;
                }
            }
        }

        // SFX 슬라이더 값 변경 감지 및 볼륨 업데이트
        if (SFXslider != null)
        {
            float currentSFXValue = SFXslider.value;
            if (currentSFXValue != SFXvolume)
            {
                // 슬라이더 값이 변경되었으므로 모든 SFX AudioSource 볼륨 업데이트
                OnSFXVolumeChanged(currentSFXValue);
                SFXvolume = currentSFXValue; // 이전 값 업데이트
                Game_Manager.Instance.sfxVolumeValue = SFXvolume;
                if(!Game_Manager.Instance.isSfxVolumeValueSet)
                {
                    Game_Manager.Instance.isSfxVolumeValueSet = true;
                }
            }
        }
    }

    /// <summary>
    /// BGM 슬라이더 값 변경 시 호출되는 메서드
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        // slider 값(0~1)을 그대로 AudioSource volume에 적용
        foreach (AudioSource source in bgmSoundSource)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }

    /// <summary>
    /// SFX 슬라이더 값 변경 시 호출되는 메서드
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // slider 값(0~1)을 그대로 AudioSource volume에 적용
        foreach (AudioSource source in sfxSoundSource)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }

    // SFX 오브젝트 등록 - GameObject의 AudioSource를 찾아서 리스트에 추가
    public void RegisterSFX(GameObject obj)
    {
        if (obj != null)
        {
            sfxSoundSource.Add(obj.GetComponent<AudioSource>());
        }
    }

    // SFX 오브젝트 등록 해제 - GameObject의 AudioSource를 리스트에서 제거
    public void UnregisterSFX(GameObject obj)
    {
        if (obj != null)
        {
            sfxSoundSource.Remove(obj.GetComponent<AudioSource>());
        }
    }
}
