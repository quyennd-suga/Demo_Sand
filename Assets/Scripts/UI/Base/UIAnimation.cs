using System;
using UnityEngine;
using DG.Tweening;

public class UIAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private AnimationType showAnimation = AnimationType.Scale;
    [SerializeField] private AnimationType hideAnimation = AnimationType.Scale;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;

    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        SlideFromTop,
        SlideFromBottom,
        SlideFromLeft,
        SlideFromRight
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    public void PlayShowAnimation(Action onComplete = null)
    {
        PlayAnimation(showAnimation, true, showEase, onComplete);
    }

    public void PlayHideAnimation(Action onComplete = null)
    {
        PlayAnimation(hideAnimation, false, hideEase, onComplete);
    }

    private void PlayAnimation(AnimationType type, bool isShow, Ease ease, Action onComplete)
    {
        switch (type)
        {
            case AnimationType.Fade:
                AnimateFade(isShow, ease, onComplete);
                break;
            case AnimationType.Scale:
                AnimateScale(isShow, ease, onComplete);
                break;
            case AnimationType.SlideFromTop:
                AnimateSlide(Vector2.up, isShow, ease, onComplete);
                break;
            case AnimationType.SlideFromBottom:
                AnimateSlide(Vector2.down, isShow, ease, onComplete);
                break;
            case AnimationType.SlideFromLeft:
                AnimateSlide(Vector2.left, isShow, ease, onComplete);
                break;
            case AnimationType.SlideFromRight:
                AnimateSlide(Vector2.right, isShow, ease, onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    private void AnimateFade(bool isShow, Ease ease, Action onComplete)
    {
        if (canvasGroup == null) return;

        float targetAlpha = isShow ? 1f : 0f;
        canvasGroup.DOFade(targetAlpha, animationDuration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void AnimateScale(bool isShow, Ease ease, Action onComplete)
    {
        Vector3 targetScale = isShow ? originalScale : Vector3.zero;
        
        if (!isShow)
            transform.localScale = originalScale;
        else
            transform.localScale = Vector3.zero;

        transform.DOScale(targetScale, animationDuration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void AnimateSlide(Vector2 direction, bool isShow, Ease ease, Action onComplete)
    {
        if (rectTransform == null) return;

        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 offScreenPosition = originalPosition + direction * Screen.height;

        if (!isShow)
            rectTransform.anchoredPosition = originalPosition;
        else
            rectTransform.anchoredPosition = offScreenPosition;

        Vector2 targetPosition = isShow ? originalPosition : offScreenPosition;

        rectTransform.DOAnchorPos(targetPosition, animationDuration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }
}
