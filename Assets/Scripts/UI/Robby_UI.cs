using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Robby_UI : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerLevelText;
    public TextMeshProUGUI stageText;
    public GameObject deleteUserDataButton;
    public GameObject exitButton;
    public GameObject goInGameButton;
    public Image stageImage;
    public List<Sprite> stageSprites;
    public TextMeshProUGUI stageNameText;
    public GameObject settingUI;
    public GameObject stageLockIcon;
    public Game_Manager game_Manager;
    public float settingUIAnimDuration = 0.35f;

    private Transform _settingContent;
    private Coroutine _settingUICoroutine;
    private Coroutine _buttonAnimationCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        _settingContent = settingUI.transform.GetChild(1);
        settingUI.SetActive(false);
        game_Manager = Game_Manager.Instance;
        
        // deleteUserDataButton의 onClick에 DeleteCurrentPlayerData 메서드 연결
        if (deleteUserDataButton != null)
        {
            Button button = deleteUserDataButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(DeleteUserDataButton);
            }
        }
        if(exitButton != null)
        {
            Button button = exitButton.GetComponent<Button>();
            if(button != null)
            {
                button.onClick.AddListener(ExitButton);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExitButton()
    {
        if(game_Manager != null)
        {
            game_Manager.ExitGame();
        }
    }

    public async void DeleteUserDataButton()
    {
        if (game_Manager != null && game_Manager.userData != null)
        {
            await game_Manager.userData.DeleteCurrentPlayerData();
        }
        Debug.Log("플레이어 데이터 삭제 완료");
        SceneManager.LoadScene("Title");
    }

    public void SettingUI()
    {
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
}
