using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomButtonEffect : CustomButtonBase
{
    #region Variables

    private const float OriginalScale = 1.0f;
    [SerializeField] private float toScale;
    [SerializeField] private float duration;

    #endregion

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        Debug.Log("OnPointerDown");
        transform.DOScale(toScale, duration)
            .SetEase(Ease.InOutSine);
    }


    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        transform.DOScale(OriginalScale, duration)
            .SetEase(Ease.InOutSine);
    }
}
