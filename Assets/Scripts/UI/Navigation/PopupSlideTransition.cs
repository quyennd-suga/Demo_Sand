using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class PopupSlideTransition : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 1024;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
   
    private TabType currentTab = TabType.Home;
    private bool isTransitioning = false;
    
    public static event Action<TabType> OnSlideTransitionComplete;
    

    public void SlideToTab(TabType fromTab, TabType toTab)
    {
        if (isTransitioning || fromTab == toTab) return;
        
        isTransitioning = true;
        SlideDirection direction = GetSlideDirection(fromTab, toTab);
        PerformSlideTransition(fromTab, toTab, direction);
    }
    
    private SlideDirection GetSlideDirection(TabType fromTab, TabType toTab)
    {
        int fromIndex = GetTabIndex(fromTab);
        int toIndex = GetTabIndex(toTab);
        return toIndex > fromIndex ? SlideDirection.Left : SlideDirection.Right;
    }
    
    private int GetTabIndex(TabType tabType)
    {
        switch (tabType)
        {
            case TabType.Home: return 1;
            case TabType.Shop: return 0;
            case TabType.ComingSoon: return 2;
            default: return 1;
        }
    }
    
    private void PerformSlideTransition(TabType fromTab, TabType toTab, SlideDirection direction)
    {
        PopupUI fromPopup = GetPopupForTab(fromTab);
        PopupUI toPopup = GetPopupForTab(toTab);
        SetupSlidePositions(fromPopup, toPopup, direction);
        if (toPopup != null)
        {
            toPopup.gameObject.SetActive(true);

            RectTransform medicalRect = toPopup.GetComponent<RectTransform>();
            float startOffset = direction == SlideDirection.Left ? slideDistance : -slideDistance;
            medicalRect.anchoredPosition = new Vector2(startOffset, medicalRect.anchoredPosition.y);

            toPopup.Show();
        }
        
        AnimateSlide(fromPopup, toPopup, direction, () =>
        {
            if (fromPopup != null)
            {
                fromPopup.Hide();
            }
            
            currentTab = toTab;
            isTransitioning = false;
            OnSlideTransitionComplete?.Invoke(toTab);
        });
    }
    
    private PopupUI GetPopupForTab(TabType tabType)
    {
        PopupUI popup=null;
        switch (tabType)
        {
            case TabType.Home:
                popup = null;
                break;

            case TabType.Shop:
                UIManager.Instance.ShowPopup<PopupShop>();
                popup = UIManager.Instance.GetPopup<PopupShop>();
                break;

            case TabType.ComingSoon:
                UIManager.Instance.ShowPopup<PopupCommingSoon>();
                popup = UIManager.Instance.GetPopup<PopupCommingSoon>();
                break;
            default:
                break;
        }
        return popup;

    }
    
    private void SetupSlidePositions(PopupUI fromPopup, PopupUI toPopup, SlideDirection direction)
    {

        float startOffset = direction == SlideDirection.Left ? slideDistance : -slideDistance;
         if (toPopup != null)
        {
            RectTransform toRect = toPopup.GetComponent<RectTransform>();
            if (toRect != null)
            {
                toRect.anchoredPosition = new Vector2(startOffset, toRect.anchoredPosition.y);
            }
        }

        if (fromPopup != null)
        {
            RectTransform fromRect = fromPopup.GetComponent<RectTransform>();
            if (fromRect != null)
            {
                fromRect.anchoredPosition = new Vector2(0, fromRect.anchoredPosition.y);
            }
        }
    }
    
    private void AnimateSlide(PopupUI fromPopup, PopupUI toPopup, SlideDirection direction, Action onComplete)
    {
        Sequence slideSequence = DOTween.Sequence();
        
        float endOffset = direction == SlideDirection.Left ? -slideDistance : slideDistance;

        if (fromPopup != null)
        {
            RectTransform fromRect = fromPopup.GetComponent<RectTransform>();
            if (fromRect != null)
            {
                slideSequence.Join(fromRect.DOAnchorPosX(endOffset, slideDuration).SetEase(slideEase));
            }
        }
        if (toPopup != null)
        {
            RectTransform toRect = toPopup.GetComponent<RectTransform>();
            if (toRect != null)
            {
                slideSequence.Join(toRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase));
            }
        }
        
        slideSequence.OnComplete(() => onComplete?.Invoke());
    }
    
    public void ShowPopupWithSlide(TabType targetTab)
    {
        SlideToTab(currentTab, targetTab);
    }
    
    public void HideCurrentPopupWithSlide(TabType targetTab = TabType.Home)
    {
        if (currentTab != TabType.Home)
        {
            PopupUI currentPopup = GetPopupForTab(currentTab);
            if (currentPopup != null)
            {
                if (targetTab == TabType.Home)
                {
                    SlidePopupOut(currentPopup, () =>
                    {
                        currentPopup.Hide();
                        currentTab = TabType.Home;
                        OnSlideTransitionComplete?.Invoke(TabType.Home);
                    });
                }
                else
                {
                    SlideToTab(currentTab, targetTab);
                }
            }
        }
    }
    
    
    private void SlidePopupOut(PopupUI popup, Action onComplete)
    {
        if (popup == null) return;
        
        isTransitioning = true;
        
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.DOAnchorPosX(slideDistance, slideDuration)
                .SetEase(slideEase)
                .OnComplete(() =>
                {
                    isTransitioning = false;
                    onComplete?.Invoke();
                });
        }
        else
        {
            isTransitioning = false;
            onComplete?.Invoke();
        }
    }
    
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}

public enum SlideDirection
{
    Left,
    Right
}