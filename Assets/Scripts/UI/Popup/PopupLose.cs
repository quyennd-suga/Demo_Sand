using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Spine.Unity;

public class PopupLose : PopupUI
{
    [SerializeField] private Image boosterBtn1;
    [SerializeField] private Image boosterBtn2;
    [SerializeField] private Image boosterBtn3;
    [SerializeField] private Image retryBtn; 

    [SerializeField] private SkeletonGraphic skeleton1;
    [SerializeField] private SkeletonGraphic skeleton2;

    private Sequence button4LoopSequence; 

    public override void Initialize(UIManager manager)
    {

    }

    protected override void OnShow()
    {
        base.OnShow();
        // Logic khi popup hiển thị
        //PlayButtonAnimations();
    }
    void OnEnable()
    {
        PlayButtonAnimations();
    }
    // Xóa OnEnable để tránh gọi 2 lần
    [ContextMenu("Test Anim")]
    void TestAnim()
    {
        PlayButtonAnimations();
    }
    private void PlayButtonAnimations()
    {
        // Kill các animation cũ nếu có
        KillTween();

        // Set scale về 0 ngay lập tức
        boosterBtn1.transform.localScale = Vector3.zero;
        boosterBtn2.transform.localScale = Vector3.zero;
        boosterBtn3.transform.localScale = Vector3.zero;
        retryBtn.transform.localScale = Vector3.zero;

        boosterBtn1.transform.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce);
        boosterBtn2.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBounce).SetDelay(0.15f);
        boosterBtn3.transform.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce);
        retryBtn.transform.DOScale(Vector3.one, 1.2f).SetEase(Ease.OutBounce).OnComplete(() => 
        {
            button4LoopSequence?.Kill();
            button4LoopSequence = DOTween.Sequence();
            button4LoopSequence.AppendCallback(() => { skeleton1.timeScale = 1; skeleton2.timeScale = 1; });
            button4LoopSequence.Append(retryBtn.transform.DOScale(new Vector3(1.04f, 1.04f, 1f), 0.7f).SetEase(Ease.InOutQuad));
            button4LoopSequence.Append(retryBtn.transform.DOScale(Vector3.one, 0.7f).SetEase(Ease.InOutQuad));
            button4LoopSequence.AppendCallback(() => { skeleton1.timeScale = 0; skeleton2.timeScale = 0; });
            button4LoopSequence.Append(retryBtn.transform.DOPunchPosition(new Vector3(0, 12f, 0), 0.6f, 0, 0).SetEase(Ease.Linear));
            button4LoopSequence.Append(retryBtn.transform.DOPunchPosition(new Vector3(0, 12f, 0), 0.6f, 0, 0).SetEase(Ease.Linear));
            button4LoopSequence.SetLoops(-1);
        });
    }
    private void KillTween()
    {
        // Kill sequence loop trước
        button4LoopSequence?.Kill();
        
        // Kill các tween trên transform
        boosterBtn1.transform.DOKill();
        boosterBtn2.transform.DOKill();
        boosterBtn3.transform.DOKill();
        retryBtn.transform.DOKill();
    }
    protected override void OnHide()
    {
        base.OnHide();
        // Kill tất cả animation khi đóng popup
        KillTween();
    }


}
