using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
public class CoinDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Button addCoinButton;
    [SerializeField] private Image coinIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool showGainEffect = true;
    
    [Header("Formatting")]
    [SerializeField] private bool useShortFormat = true; 
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";
    
    private int currentDisplayedCoins;
    private Tween coinTween;
    
    private void Start()
    {
        GameDataManager.OnCoinsChanged += OnCoinsChanged;
                if (addCoinButton != null)
            addCoinButton.onClick.AddListener(OnAddCoinClicked);
        
        UpdateDisplay();
    }
    
    private void OnDestroy()
    {
        if (GameDataManager.Instance != null)
            GameDataManager.OnCoinsChanged -= OnCoinsChanged;
        
        if (addCoinButton != null)
            addCoinButton.onClick.RemoveListener(OnAddCoinClicked);
        
        coinTween?.Kill();
    }
    
    private void UpdateDisplay()
    {
        if (GameDataManager.Instance == null) return;
        
        currentDisplayedCoins = GameDataManager.Instance.GetCoins();
        UpdateCoinText(currentDisplayedCoins);
    }
    
    private void UpdateCoinText(int coins)
    {
        if (coinText == null) return;
        
        string formattedCoins = useShortFormat ? FormatNumber(coins) : coins.ToString();
        coinText.text = $"{prefix}{formattedCoins}{suffix}";
    }
    
    private string FormatNumber(int number)
    {
        if (number >= 1000000)
            return (number / 1000000f).ToString("0.0") + "M";
        else if (number >= 1000)
            return (number / 1000f).ToString("0.0") + "K";
        else
            return number.ToString();
    }
    
    #region Event Handlers
    
    private void OnCoinsChanged(int newAmount)
    {
        if (animateChanges)
        {
            AnimateCoinChange(currentDisplayedCoins, newAmount);
        }
        else
        {
            currentDisplayedCoins = newAmount;
            UpdateCoinText(currentDisplayedCoins);
        }
        
        // Show gain effect if coins increased
        if (showGainEffect && newAmount > currentDisplayedCoins)
        {
            ShowGainEffect(newAmount - currentDisplayedCoins);
        }
    }
    
    private void OnAddCoinClicked()
    {
        Debug.Log("Add coins clicked - Show shop or watch ads");
    }
    
    #endregion
    
    #region Animation
    
    private void AnimateCoinChange(int from, int to)
    {
        coinTween?.Kill();
        
        if (coinIcon != null)
        {
            coinIcon.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f);
        }
        
        coinTween = DOTween.To(
            () => from,
            (value) => UpdateCoinText(value),
            to,
            animationDuration
        ).SetEase(Ease.OutQuart)
        .OnComplete(() => {
            currentDisplayedCoins = to;
            UpdateCoinText(currentDisplayedCoins);
        });
    }
    
    private void ShowGainEffect(int gainAmount)
    {
        Debug.Log($"+{gainAmount} coins!");
    }
    
    #endregion
    
    #region Public Methods
    

    public void RefreshDisplay()
    {
        UpdateDisplay();
    }
    
    public void SetFormatting(bool shortFormat, string newPrefix = "", string newSuffix = "")
    {
        useShortFormat = shortFormat;
        prefix = newPrefix;
        suffix = newSuffix;
        UpdateDisplay();
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        animateChanges = enabled;
    }
    
    public void TriggerGainEffect(int amount)
    {
        ShowGainEffect(amount);
    }
    
    #endregion
}