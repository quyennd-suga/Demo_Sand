using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class HeartDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI heartText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button addHeartButton;
    [SerializeField] private Image heartIcon;
    [SerializeField] private Image timerIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool showSpendEffect = true;
    
    [Header("Timer Settings")]
    [SerializeField] private bool showTimer = true;
    [SerializeField] private bool infiniteHearts = false;
    
    private int currentDisplayedHearts;
    private int maxHearts;
    private Tween heartTween;
    
    private void Start()
    {
        GameDataManager.OnHeartsChanged += OnHeartsChanged;
                if (addHeartButton != null)
            addHeartButton.onClick.AddListener(OnAddHeartClicked);
        
        maxHearts = 5; 
                UpdateDisplay();
                if (showTimer && timerText != null)
            InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
    }
    
    private void OnDestroy()
    {
        if (GameDataManager.Instance != null)
            GameDataManager.OnHeartsChanged -= OnHeartsChanged;
        
        if (addHeartButton != null)
            addHeartButton.onClick.RemoveListener(OnAddHeartClicked);
        
        heartTween?.Kill();
    }
    
    private void UpdateDisplay()
    {
        if (GameDataManager.Instance == null) return;
        
        currentDisplayedHearts = GameDataManager.Instance.GetHearts();
        UpdateHeartText(currentDisplayedHearts);
    }
    
    private void UpdateHeartText(int hearts)
    {
        if (heartText == null) return;
        
        if (infiniteHearts)
        {
            heartText.text = "∞";
        }
        else if (hearts >= maxHearts)
        {
            heartText.text = $"{hearts}";
        }
        else
        {
            heartText.text = $"{hearts}";
        }
    }
    
    private void UpdateTimer()
    {
        if (!showTimer || timerText == null || infiniteHearts) return;
        
        var dataManager = GameDataManager.Instance;
        if (dataManager == null) return;
        
        var timeUntilNext = dataManager.GetTimeUntilNextHeart();
        
        if (timeUntilNext == TimeSpan.Zero || currentDisplayedHearts >= maxHearts)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "Full";
            if (timerIcon != null)
                timerIcon.gameObject.SetActive(false);
        }
        else
        {
            timerText.gameObject.SetActive(true);
            if (timerIcon != null)
                timerIcon.gameObject.SetActive(true);
            
            timerText.text = $"{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
        }
    }
    
    #region Event Handlers
    
    private void OnHeartsChanged(int newAmount)
    {
        bool heartsDecreased = newAmount < currentDisplayedHearts;
        
        if (animateChanges)
        {
            AnimateHeartChange(currentDisplayedHearts, newAmount);
        }
        else
        {
            currentDisplayedHearts = newAmount;
            UpdateHeartText(currentDisplayedHearts);
        }
        
        if (showSpendEffect && heartsDecreased)
        {
            ShowSpendEffect();
        }
    }
    
    private void OnAddHeartClicked()
    {
        Debug.Log("Add hearts clicked - Show heart purchase options");

    }
    
    #endregion
    
    #region Animation
    
    private void AnimateHeartChange(int from, int to)
    {
        heartTween?.Kill();
        
        if (heartIcon != null)
        {
            if (to < from)
            {
                heartIcon.transform.DOShakePosition(0.5f, 10f, 20, 90f);
            }
            else
            {
                heartIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 1, 0.5f);
            }
        }
        
        heartTween = DOTween.To(
            () => from,
            (value) => UpdateHeartText(value),
            to,
            animationDuration
        ).SetEase(Ease.OutQuart)
        .OnComplete(() => {
            currentDisplayedHearts = to;
            UpdateHeartText(currentDisplayedHearts);
        });
    }
    
    private void ShowSpendEffect()
    {
        Debug.Log("Heart spent!");
            }
    
    #endregion
    
    #region Public Methods
    

    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    public void SetMaxHearts(int max)
    {
        maxHearts = max;
        UpdateDisplay();
    }

    public void SetTimerEnabled(bool enabled)
    {
        showTimer = enabled;
        
        if (!enabled)
        {
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            if (timerIcon != null)
                timerIcon.gameObject.SetActive(false);
        }
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        animateChanges = enabled;
    }
    
    public void SetInfiniteHearts(bool infinite)
    {
        infiniteHearts = infinite;
        UpdateDisplay();
        
        if (infinite && timerText != null)
        {
            timerText.gameObject.SetActive(false);
            if (timerIcon != null)
                timerIcon.gameObject.SetActive(false);
        }
    }
    
    public bool IsInfiniteHearts()
    {
        return infiniteHearts;
    }
    
    public bool CanPlay()
    {
        return infiniteHearts || GameDataManager.Instance.CanSpendHearts(1);
    }
    
    public string GetNextHeartTimeText()
    {
        if (infiniteHearts) return "∞";
        
        var timeUntilNext = GameDataManager.Instance.GetTimeUntilNextHeart();
        if (timeUntilNext == TimeSpan.Zero)
            return "Full";
        
        return $"{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
    }
    
    #endregion
}