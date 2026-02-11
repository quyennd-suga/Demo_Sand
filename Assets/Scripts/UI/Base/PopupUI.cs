using System;
using UnityEngine;

public abstract class PopupUI : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected GameObject overlay;
    
    protected UIManager uiManager;
    
    public bool IsActive { get; private set; }
    public string PopupName { get; protected set; }

    protected virtual void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        PopupName = GetType().Name;
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
        
        if (overlay != null)
            overlay.SetActive(true);
        
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
        
        if (overlay != null)
            overlay.SetActive(false);
        
        OnHide();
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    
    public virtual void Close()
    {
        Hide();
    }
}
