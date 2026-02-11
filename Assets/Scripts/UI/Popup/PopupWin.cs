using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PopupWin : PopupUI
{
    [SerializeField] private RectTransform loopButton;
    
    private Tween loopTween;

    void OnEnable()
    {
        PlayLoopAnimation();
    }

    private void PlayLoopAnimation()
    {
        // Kill animation cũ nếu có
        loopTween?.Kill();

        // Reset scale về giá trị ban đầu
        loopButton.localScale = Vector3.one;

        // Scale animation với loop yoyo
        loopTween = loopButton.DOScale(new Vector3(1.1f, 1.1f, 1f), 1f)
            .SetEase(Ease.Unset)
            .SetLoops(-1, LoopType.Yoyo);
    }

    protected override void OnHide()
    {
        base.OnHide();
        // Kill animation khi đóng popup
        loopTween?.Kill();
    }
}
