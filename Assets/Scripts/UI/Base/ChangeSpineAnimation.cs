using Spine;
using Spine.Unity;
using System.Collections;
using UnityEngine;
public class ChangeSpineAnimation : MonoBehaviour
{
    private SkeletonGraphic skeletonAnimation;
    public string startname = "Start";
    public string loopname = "Idle";

    private void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonGraphic>();
    }
    private void OnEnable()
    {
        StartCoroutine(PlayStartAnimationNextFrame());

    }
    private void OnDisable()
    {
        skeletonAnimation.AnimationState.SetAnimation(0, startname, false);
        skeletonAnimation.Update(0);
        skeletonAnimation.AnimationState.ClearTracks();
    }

    void AnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry.Animation.Name == startname)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, loopname, true);
        }
    }
    IEnumerator PlayStartAnimationNextFrame()
    {
        yield return null;
        skeletonAnimation.AnimationState.Complete += AnimationComplete;
        skeletonAnimation.Skeleton.SetToSetupPose();
        skeletonAnimation.AnimationState.SetAnimation(0, startname, false);
    }
}
