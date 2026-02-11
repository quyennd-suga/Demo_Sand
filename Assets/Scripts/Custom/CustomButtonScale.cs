using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CustomButtonScale : CustomButtonBase
{
    #region Variables

    private float OriginalScale = 1.0f;
    [SerializeField] private float toScale;
    [SerializeField] private float duration;

    #endregion

    //public override void OnPointerEnter(PointerEventData eventData)
    //{
    //    base.OnPointerEnter(eventData);

    //    transform.DOScale(toScale, duration)
    //        .SetEase(Ease.InOutSine);
    //}
    public void ConfigScale(float scale,float original)
    {
        toScale = scale;
        OriginalScale = original;
    }    
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        transform.DOKill();
        transform.DOScale(toScale, duration)
            .SetEase(Ease.InOutSine);
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        transform.DOKill();
        transform.DOScale(OriginalScale, duration)
            .SetEase(Ease.InOutSine);
    }
    //public override void OnPointerExit(PointerEventData eventData)
    //{
    //    base.OnPointerExit(eventData);
    //    transform.DOKill();
    //    transform.DOScale(OriginalScale, duration)
    //        .SetEase(Ease.InOutSine);
    //}
}
