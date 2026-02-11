using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

public class PopupWarningHard : PopupUI
{
    [SerializeField] private SkeletonGraphic spineGraphic;
    
#region Test Code
    void OnEnable()
    {
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        spineGraphic.AnimationState.Complete += OnAnimationComplete;
        spineGraphic.AnimationState.ClearTracks();
        spineGraphic.AnimationState.SetAnimation(0, "Anim", false);
    }
    void OnDisable()
    {
        spineGraphic.AnimationState.Complete -= OnAnimationComplete;
    }
#endregion

    protected override void OnShow()
    {
        base.OnShow();
        spineGraphic.AnimationState.Complete += OnAnimationComplete;
        spineGraphic.AnimationState.ClearTracks();
        spineGraphic.AnimationState.SetAnimation(0, "Anim", false);
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        spineGraphic.AnimationState.Complete -= OnAnimationComplete;
    }

    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        Hide();
    }
}
