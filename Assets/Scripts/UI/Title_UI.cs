using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Title_UI : MonoBehaviour
{
    public GameObject loginButtons;
    public GameObject gameStartButton;
    public GameObject gameManager;
    public User_Data userData;
    public GameObject settingUI;
    public float settingUIAnimDuration = 0.35f;

    private Transform _settingContent;
    private Coroutine _settingUICoroutine;
    private Coroutine _gameStartButtonCoroutine;
    private Coroutine _buttonAnimationCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager");
        gameManager.GetComponent<Game_Manager>().gameState = Game_Manager.GameState.Title;
        userData = gameManager.GetComponent<User_Data>();
        _settingContent = settingUI.transform.GetChild(1);
        settingUI.SetActive(false);
        gameStartButton.SetActive(false);
    }
    
    public void GuestLogin()
    {
        // 비동기 로그인 처리
        StartCoroutine(GuestLoginCoroutine());
    }

    public void MusicButton()
    {
        // 닫을 때는 바로 비활성화, 열 때만 이징 애니메이션 적용
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

    private IEnumerator GuestLoginCoroutine()
    {
        bool isCompleted = false;
        bool isSuccess = false;
        
        // 비동기 로그인 시작
        userData.GuestLogin().ContinueWith(task =>
        {
            // GuestLogin이 반환한 성공 여부를 사용 (예외는 GuestLogin 안에서 처리됨)
            isSuccess = !task.IsFaulted && task.Result;
            isCompleted = true;
        });

        // 로그인 완료까지 대기
        while (!isCompleted)
        {
            yield return null;
        }

        // 로그인 성공 시에만 UI 변경
        if (isSuccess)
        {
            loginButtons.SetActive(false);
            // gameStartButton을 EaseOutBack 애니메이션과 함께 활성화
            /*
            if (_gameStartButtonCoroutine != null)
            {
                StopCoroutine(_gameStartButtonCoroutine);
            }
            */
            _gameStartButtonCoroutine = StartCoroutine(ShowGameStartButtonWithEase());
            Debug.Log("게스트 로그인 성공 - UI 업데이트 완료");
        }
        else
        {
            Debug.LogError("게스트 로그인 실패 - UI 변경하지 않음");
        }
    }

    public void DeleteAllDataButton()
    {
        userData.DeleteAllData();
        loginButtons.SetActive(true);
        gameStartButton.SetActive(false);
    }

    public void GoRobby()
    {
        SceneManager.LoadScene("Robby");
        //gameManager.GetComponent<Game_Manager>().gameState = Game_Manager.GameState.Robby;
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

    private IEnumerator ShowGameStartButtonWithEase()
    {
        if (gameStartButton == null) yield break;
        
        gameStartButton.SetActive(true);
        gameStartButton.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < settingUIAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settingUIAnimDuration);
            float eased = EaseOutBack(t);
            gameStartButton.transform.localScale = Vector3.one * eased;
            yield return null;
        }

        gameStartButton.transform.localScale = Vector3.one;
        _gameStartButtonCoroutine = null;
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
