using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
public class HoldToSeeBoard :MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hold to See Board")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float hideAlpha = 0f;

    private bool isHolding = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isHolding)
        {
            isHolding = true;
            HidePopupTemporary();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHolding)
        {
            isHolding = false;
            ShowPopupAgain();
        }
    }

    private void HidePopupTemporary()
    {
        popupCanvasGroup.DOKill();
        popupCanvasGroup.DOFade(hideAlpha, fadeDuration).SetEase(Ease.OutQuad);
    }

    private void ShowPopupAgain()
    {
        popupCanvasGroup.DOKill();
        popupCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
    }
}