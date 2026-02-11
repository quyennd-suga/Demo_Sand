using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using DG.Tweening;
using TMPro;
public class TabNavigationManager : MonoBehaviour
{
    [Header("Tab Navigation UI")]
    [SerializeField] private RectTransform hightlightRect;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Button comingSoonTabButton;
    [SerializeField] private TextMeshProUGUI homeFooterTxt;
    [SerializeField] private TextMeshProUGUI shopFooterTxt;
    [SerializeField] private TextMeshProUGUI rankFooterTxt;
    [SerializeField] private Image homeFooterImg;
    [SerializeField] private Image shopFooterImg;
    [SerializeField] private Image rankFooterImg;
    
    [Header("Slide Transition")]
    [SerializeField] private PopupSlideTransition slideTransition;
    [SerializeField] private bool useSlideTransition = true;

    private TabType currentTab = TabType.Home;
    private readonly Dictionary<TabType, Button> tabButtons = new Dictionary<TabType, Button>();
    
    // Events
    public static event Action<TabType> OnTabChanged;
    
    private void Start()
    {
        SetupTabs();
        SetActiveTab(TabType.Home);
        
        // Subscribe to slide transition events
        if (slideTransition != null)
        {
            PopupSlideTransition.OnSlideTransitionComplete += OnSlideTransitionComplete;
        }
    }
    
    private void SetupTabs()
    {
        tabButtons[TabType.Home] = homeTabButton;
        tabButtons[TabType.Shop] = shopTabButton;
        tabButtons[TabType.ComingSoon] = comingSoonTabButton;
        
 
        // Setup button listeners
        if (homeTabButton != null)
            homeTabButton.onClick.AddListener(() => OnTabClicked(TabType.Home));
        
        if (shopTabButton != null)
            shopTabButton.onClick.AddListener(() => OnTabClicked(TabType.Shop));
        
        if (comingSoonTabButton != null)
            comingSoonTabButton.onClick.AddListener(() => OnTabClicked(TabType.ComingSoon));
    }
    
    private void OnTabClicked(TabType tabType)
    {
        if (currentTab == tabType) return;
        
        if (useSlideTransition && slideTransition != null && IsInHomeScreen())
        {
            AnimateTabUI(tabType);
            slideTransition.ShowPopupWithSlide(tabType);
        }
        else if (useSlideTransition && slideTransition != null && !IsInHomeScreen())
        {
            // Đang ở popup khác, slide sang popup mới
            AnimateTabUI(tabType);
            slideTransition.ShowPopupWithSlide(tabType);
            Debug.Log("222");
        }
        else
        {
            CloseCurrentContent();
            SetActiveTab(tabType);
            ShowTabContent(tabType);
            OnTabChanged?.Invoke(tabType);
        }
    }
    
    private bool IsInHomeScreen()
    {
        return true;
    }
    
    private void OnSlideTransitionComplete(TabType tabType)
    {
        currentTab = tabType;
        OnTabChanged?.Invoke(tabType);
    }
    
    private void CloseCurrentContent()
    {
        switch (currentTab)
        {
            case TabType.Home:
                //UIManager.Instance.Hide<PopupHome>();
                break;
                
            case TabType.Shop:
                UIManager.Instance.HidePopup<PopupShop>();
                break;
                
            case TabType.ComingSoon:
                UIManager.Instance.HidePopup<PopupCommingSoon>();
                break;
        }
    }
    
    private void ShowTabContent(TabType tabType)
    {
        switch (tabType)
        {
            case TabType.Home:
                OnHomeButtonClick();
                break;
                
            case TabType.Shop:
                OnShopButtonClick();
                break;
                
            case TabType.ComingSoon:
                OnRankButtonClick();
                break;
        }
    }
    private void AnimateTabUI(TabType tabType)
    {
        switch (tabType)
        {
            case TabType.Home:
                AnimateToHome();
                break;
            case TabType.Shop:
                AnimateToShop();
                break;
            case TabType.ComingSoon:
                AnimateToComingSoon();
                break;
        }
    }
    
    private void AnimateToHome()
    {
        Sequence seq = DOTween.Sequence();
        hightlightRect.DOKill();

        seq.Append(hightlightRect.DOAnchorPosX(0f, 0.15f).SetEase(Ease.Linear))
            .Join(homeFooterImg.rectTransform.DOAnchorPos(new Vector2(homeFooterImg.rectTransform.anchoredPosition.x, 100f), 0.1f).SetEase(Ease.Linear))
            .Join(homeFooterImg.transform.DOScale(new Vector3(1,1,1), 0.1f).SetEase(Ease.Linear))
            .Join(homeFooterTxt.DOFade(1f, 0.1f).SetEase(Ease.Linear))
            .JoinCallback(() =>
            {
                OnShopButtonUnActive();
                OnRankButtonUnActive();
            });
    }
    
    private void AnimateToShop()
    {
        Sequence seq = DOTween.Sequence();
        hightlightRect.DOKill();

        seq.Append(hightlightRect.DOMoveX(shopFooterImg.transform.position.x, 0.15f).SetEase(Ease.Linear))
            .Join(shopFooterImg.rectTransform.DOAnchorPos(new Vector2(shopFooterImg.rectTransform.localPosition.x, 100f), 0.1f).SetEase(Ease.Linear))
             .Join(shopFooterImg.transform.DOScale(new Vector3(1, 1, 1), 0.1f).SetEase(Ease.Linear))
            .Join(shopFooterTxt.DOFade(1f, 0.1f).SetEase(Ease.Linear))
            .JoinCallback(() =>
            {
                OnHomeButtonUnActive();
                OnRankButtonUnActive();
            });
    }
    
    private void AnimateToComingSoon()
    {
        Sequence seq = DOTween.Sequence();
        hightlightRect.DOKill();

        seq.Append(hightlightRect.DOMoveX(rankFooterImg.transform.position.x, 0.15f).SetEase(Ease.Linear))
            .Join(rankFooterImg.rectTransform.DOAnchorPos(new Vector2(rankFooterImg.rectTransform.localPosition.x, 100f), 0.1f).SetEase(Ease.Linear))
            .Join(rankFooterImg.transform.DOScale(new Vector3(1, 1, 1), 0.1f).SetEase(Ease.Linear))
            .Join(rankFooterTxt.DOFade(1f, 0.1f).SetEase(Ease.Linear))
            .JoinCallback(() =>
            {
                OnHomeButtonUnActive();
                OnShopButtonUnActive();
            });
    }

    public void OnHomeButtonClick()
    {
        OnTabClicked(TabType.Home);
    }

    public void OnShopButtonClick()
    {
        OnTabClicked(TabType.Shop);
    }

    public void OnRankButtonClick()
    {
        OnTabClicked(TabType.ComingSoon);
    }

    private void OnHomeButtonUnActive()
    {
        homeFooterImg.rectTransform.DOAnchorPos(new Vector2(homeFooterImg.rectTransform.anchoredPosition.x, -10f), 0.1f).SetEase(Ease.Linear);
        homeFooterImg.transform.DOScale(new Vector3(0.65f, 0.65f, 0.65f), 0.1f).SetEase(Ease.Linear);
        homeFooterTxt.DOFade(0f, 0.1f).SetEase(Ease.Linear);
    }

    private void OnShopButtonUnActive()
    {
        shopFooterImg.rectTransform.DOAnchorPos(new Vector2(shopFooterImg.rectTransform.anchoredPosition.x, -10f), 0.1f).SetEase(Ease.Linear);
        shopFooterImg.transform.DOScale(new Vector3(0.65f, 0.65f, 0.65f), 0.1f).SetEase(Ease.Linear);
        shopFooterTxt.DOFade(0f, 0.1f).SetEase(Ease.Linear);
    }
    private void OnRankButtonUnActive()
    {
        rankFooterImg.rectTransform.DOAnchorPos(new Vector2(rankFooterImg.rectTransform.anchoredPosition.x, -10f), 0.1f).SetEase(Ease.Linear);
        rankFooterImg.transform.DOScale(new Vector3(0.65f,0.65f,0.65f),0.1f).SetEase(Ease.Linear);
        rankFooterTxt.DOFade(0f, 0.1f).SetEase(Ease.Linear);
    }
    private void SetActiveTab(TabType tabType)
    {
        currentTab = tabType;
    }
    
    
    #region Public Methods

    public void SwitchToTab(TabType tabType)
    {
        OnTabClicked(tabType);
    }
    
    public TabType GetCurrentTab()
    {
        return currentTab;
    }
    public bool IsTabActive(TabType tabType)
    {
        return currentTab == tabType;
    }
   
    public void SetTabEnabled(TabType tabType, bool enabled)
    {
        if (tabButtons.TryGetValue(tabType, out Button button))
        {
            if (button != null)
                button.interactable = enabled;
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (slideTransition != null)
        {
            PopupSlideTransition.OnSlideTransitionComplete -= OnSlideTransitionComplete;
        }
        
        if (homeTabButton != null)
            homeTabButton.onClick.RemoveAllListeners();
        
        if (shopTabButton != null)
            shopTabButton.onClick.RemoveAllListeners();
        
        if (comingSoonTabButton != null)
            comingSoonTabButton.onClick.RemoveAllListeners();
    }
    
    #endregion
}

public enum TabType
{
    Home,
    Shop,
    ComingSoon
}