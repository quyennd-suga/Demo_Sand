using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButtonBackground : CustomButtonBase
{
    #region Variables

    private Image _backgroundImage;
    [SerializeField] private float duration;

    #endregion

    private void Awake() => _backgroundImage = GetComponentInChildren<Image>();

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        _backgroundImage.DOFade(1, duration)
            .SetEase(Ease.InOutSine);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        
        _backgroundImage.DOFade(0, duration)
            .SetEase(Ease.InOutSine);
    }
}
