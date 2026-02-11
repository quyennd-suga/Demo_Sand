using System;
using UnityEngine;

public abstract class ScreenUI : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    
    protected UIManager uiManager;
    
    public bool IsActive { get; private set; }
    public string ScreenName { get; protected set; }

    protected virtual void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        ScreenName = GetType().Name;
    }

    public virtual void Initialize(UIManager manager)
    {
        uiManager = manager;
        OnInitialize();
    }

    public virtual void Show(Action onComplete = null)
    {
        gameObject.SetActive(true);
        IsActive = true;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        OnShow();
        onComplete?.Invoke();
    }

    public virtual void Hide(Action onComplete = null)
    {
        IsActive = false;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        OnHide();
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    
    public virtual void OnBackPressed()
    {
        Hide();
    }
}
