using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PopupTimeUp : PopupUI
{
    [SerializeField] private RectTransform button1;
    [SerializeField] private RectTransform button2;
    [SerializeField] private RectTransform text1;
    [SerializeField] private RectTransform text2;

    void OnEnable()
    {
        PlayButtonAnimations();
    }
    void OnDisable()
    {
        KillTween();
    }
    private void PlayButtonAnimations()
    {
        KillTween();

        button1.localScale = Vector3.zero;
        button2.localScale = Vector3.zero;
        text1.localScale = Vector3.zero;
        text2.localScale = Vector3.zero;

        button1.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce).SetDelay(1f);
        button2.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce).SetDelay(1.2f);
        text1.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce).SetDelay(1.4f);
        text2.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce).SetDelay(1.6f);
    }
    private void KillTween()
    {
        button1.DOKill();
        button2.DOKill();
        text1.DOKill();
        text2.DOKill();
    }

    protected override void OnHide()
    {
        base.OnHide();
        KillTween();
    }
}
