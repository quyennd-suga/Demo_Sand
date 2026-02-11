using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private Transform screenContainer;
    [SerializeField] private Transform popupContainer;
    [SerializeField] private bool useAddressable = true;
    
    private Dictionary<string, ScreenUI> screenCache = new Dictionary<string, ScreenUI>();
    private Dictionary<string, PopupUI> popupCache = new Dictionary<string, PopupUI>();
    private Stack<ScreenUI> screenHistory = new Stack<ScreenUI>();
    private List<PopupUI> activePopups = new List<PopupUI>();
    
    private ScreenUI currentScreen;

    protected override void Awake()
    {
        base.Awake();
        
        if (screenContainer == null)
        {
            GameObject screenObj = new GameObject("ScreenContainer");
            screenObj.transform.SetParent(transform);
            screenContainer = screenObj.transform;
        }
        
        if (popupContainer == null)
        {
            GameObject popupObj = new GameObject("PopupContainer");
            popupObj.transform.SetParent(transform);
            popupContainer = popupObj.transform;
        }
    }

    #region Screen Management
    
    public void ShowScreen<T>(bool addToHistory = true, Action<T> onComplete = null) where T : ScreenUI
    {
        string screenName = typeof(T).Name;
        ShowScreen(screenName, addToHistory, onComplete);
    }

    public void ShowScreen<T>(string screenPath, bool addToHistory = true, Action<T> onComplete = null) where T : ScreenUI
    {
        if (screenCache.TryGetValue(screenPath, out ScreenUI cachedScreen))
        {
            ShowScreenInternal(cachedScreen as T, addToHistory);
            onComplete?.Invoke(cachedScreen as T);
            return;
        }

        if (useAddressable)
        {
            LoadScreenAddressable(screenPath, addToHistory, onComplete);
        }
        else
        {
            LoadScreenResource(screenPath, addToHistory, onComplete);
        }
    }

    private void LoadScreenAddressable<T>(string screenPath, bool addToHistory, Action<T> onComplete) where T : ScreenUI
    {
        Addressables.LoadAssetAsync<GameObject>(screenPath).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject screenObj = Instantiate(handle.Result, screenContainer);
                T screen = screenObj.GetComponent<T>();
                
                if (screen != null)
                {
                    screen.Initialize(this);
                    screenCache[screenPath] = screen;
                    ShowScreenInternal(screen, addToHistory);
                    onComplete?.Invoke(screen);
                }
                else
                {
                    Destroy(screenObj);
                }
            }
            else
            {
            }
        };
    }

    private void LoadScreenResource<T>(string screenPath, bool addToHistory, Action<T> onComplete) where T : ScreenUI
    {
        GameObject prefab = Resources.Load<GameObject>(screenPath);
        
        if (prefab != null)
        {
            GameObject screenObj = Instantiate(prefab, screenContainer);
            T screen = screenObj.GetComponent<T>();
            
            if (screen != null)
            {
                screen.Initialize(this);
                screenCache[screenPath] = screen;
                ShowScreenInternal(screen, addToHistory);
                onComplete?.Invoke(screen);
            }
            else
            {
                Destroy(screenObj);
            }
        }
        else
        {
        }
    }

    private void ShowScreenInternal(ScreenUI screen, bool addToHistory)
    {
        if (currentScreen != null)
        {
            if (addToHistory)
                screenHistory.Push(currentScreen);
            
            currentScreen.Hide();
        }

        currentScreen = screen;
        currentScreen.Show();
    }

    public void BackToPreviousScreen()
    {
        if (screenHistory.Count > 0)
        {
            ScreenUI previousScreen = screenHistory.Pop();
            
            if (currentScreen != null)
                currentScreen.Hide();
            
            currentScreen = previousScreen;
            currentScreen.Show();
        }
    }

    public void ClearScreenHistory()
    {
        screenHistory.Clear();
    }

    #endregion

    #region Popup Management

    public void ShowPopup<T>(Action<T> onComplete = null) where T : PopupUI
    {
        string popupName = typeof(T).Name;
        ShowPopup(popupName, onComplete);
    }

    public void ShowPopup<T>(string popupPath, Action<T> onComplete = null) where T : PopupUI
    {
        // Auto-close other popups when showing a new one (optional behavior)
        if (activePopups.Count > 0)
        {
            // You can choose to close all or just the last one
            // For tab-like behavior, close all existing popups
            HideAllPopups();
        }
        
        if (popupCache.TryGetValue(popupPath, out PopupUI cachedPopup))
        {
            ShowPopupInternal(cachedPopup as T);
            onComplete?.Invoke(cachedPopup as T);
            return;
        }

        if (useAddressable)
        {
            LoadPopupAddressable(popupPath, onComplete);
        }
        else
        {
            LoadPopupResource(popupPath, onComplete);
        }
    }

    private void LoadPopupAddressable<T>(string popupPath, Action<T> onComplete) where T : PopupUI
    {
        Addressables.LoadAssetAsync<GameObject>(popupPath).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                CreatePopupFromHandle(handle, popupPath, onComplete);
            }
            else
            {
                TryAlternativePopupPaths<T>(popupPath, onComplete);
            }
        };
    }

    private void TryAlternativePopupPaths<T>(string originalPath, Action<T> onComplete) where T : PopupUI
    {
        string[] possiblePaths = {
            $"UI/Popups/{originalPath}",
            $"Resources/UI/Popups/{originalPath}",
            $"Resources_moved/UI/Popups/{originalPath}",
            $"Assets/Resources/UI/Popups/{originalPath}",
            $"Assets/Resources_moved/UI/Popups/{originalPath}"
        };

        TryLoadPopupWithPaths<T>(possiblePaths, 0, originalPath, onComplete);
    }

    private void TryLoadPopupWithPaths<T>(string[] paths, int index, string originalPath, Action<T> onComplete) where T : PopupUI
    {
        if (index >= paths.Length)
        {
            return;
        }

        Addressables.LoadAssetAsync<GameObject>(paths[index]).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                CreatePopupFromHandle(handle, originalPath, onComplete);
            }
            else
            {
                TryLoadPopupWithPaths<T>(paths, index + 1, originalPath, onComplete);
            }
        };
    }

    private void CreatePopupFromHandle<T>(AsyncOperationHandle<GameObject> handle, string popupPath, Action<T> onComplete) where T : PopupUI
    {
        GameObject popupObj = Instantiate(handle.Result, popupContainer);
        T popup = popupObj.GetComponent<T>();
        
        if (popup != null)
        {
            popup.Initialize(this);
            popupCache[popupPath] = popup;
            ShowPopupInternal(popup);
            onComplete?.Invoke(popup);
        }
        else
        {
            Destroy(popupObj);
        }
    }

    private void LoadPopupResource<T>(string popupPath, Action<T> onComplete) where T : PopupUI
    {
        GameObject prefab = Resources.Load<GameObject>(popupPath);
        
        if (prefab != null)
        {
            GameObject popupObj = Instantiate(prefab, popupContainer);
            T popup = popupObj.GetComponent<T>();
            
            if (popup != null)
            {
                popup.Initialize(this);
                popupCache[popupPath] = popup;
                ShowPopupInternal(popup);
                onComplete?.Invoke(popup);
            }
            else
            {
                Destroy(popupObj);
            }
        }
        else
        {
        }
    }

    private void ShowPopupInternal(PopupUI popup)
    {
        if (!activePopups.Contains(popup))
            activePopups.Add(popup);
        
        popup.Show();
    }

    public void HidePopup<T>() where T : PopupUI
    {
        string popupName = typeof(T).Name;
        HidePopup(popupName);
    }

    public void HidePopup(string popupPath)
    {
        if (popupCache.TryGetValue(popupPath, out PopupUI popup))
        {
            popup.Hide();
            activePopups.Remove(popup);
        }
    }

    public void HideAllPopups()
    {
        foreach (var popup in activePopups)
        {
            popup.Hide();
        }
        activePopups.Clear();
    }

    public void CloseAllPopup()
    {
        HideAllPopups();
    }

    #endregion

    #region Utility

    public T GetScreen<T>() where T : ScreenUI
    {
        string screenName = typeof(T).Name;
        return screenCache.TryGetValue(screenName, out ScreenUI screen) ? screen as T : null;
    }

    public T GetPopup<T>() where T : PopupUI
    {
        string popupName = typeof(T).Name;
        return popupCache.TryGetValue(popupName, out PopupUI popup) ? popup as T : null;
    }
    
    public bool IsScreenActive<T>() where T : ScreenUI
    {
        return currentScreen != null && currentScreen is T && currentScreen.IsActive;
    }
    
    public ScreenUI GetCurrentScreen()
    {
        return currentScreen;
    }

    public void SetUseAddressable(bool value)
    {
        useAddressable = value;
    }
    
    /// <summary>
    /// Show popup without auto-closing others (for stacking popups)
    /// </summary>
    public void ShowPopupStacked<T>(Action<T> onComplete = null) where T : PopupUI
    {
        string popupName = typeof(T).Name;
        ShowPopupStacked(popupName, onComplete);
    }
    
    public void ShowPopupStacked<T>(string popupPath, Action<T> onComplete = null) where T : PopupUI
    {
        // Don't auto-close other popups
        if (popupCache.TryGetValue(popupPath, out PopupUI cachedPopup))
        {
            ShowPopupInternal(cachedPopup as T);
            onComplete?.Invoke(cachedPopup as T);
            return;
        }

        if (useAddressable)
        {
            LoadPopupAddressable(popupPath, onComplete);
        }
        else
        {
            LoadPopupResource(popupPath, onComplete);
        }
    }
    
    /// <summary>
    /// Replace current popup with new one (for tab-like behavior)
    /// </summary>
    public void ReplacePopup<T>(Action<T> onComplete = null) where T : PopupUI
    {
        // This is the default behavior now in ShowPopup
        ShowPopup<T>(onComplete);
    }

    #endregion
}
