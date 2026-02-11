using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] private PopupUnit[] popupUnits;
    private static PopupData popupData;

    private static Dictionary<PopupType, BasePopup> popupDicts;

    private static readonly HashSet<PopupType> activePopups = new();
    private static readonly List<PopupType> activeSnapshot = new(32);

    private void Awake()
    {
        popupDicts = new Dictionary<PopupType, BasePopup>(popupUnits != null ? popupUnits.Length : 8);

        if (popupUnits == null || popupUnits.Length == 0)
        {
            Debug.LogWarning("[PopupManager] popupUnits is empty.");
            return;
        }

        for (int i = 0; i < popupUnits.Length; i++)
        {
            var unit = popupUnits[i];
            if (unit == null)
            {
                Debug.LogError($"[PopupManager] popupUnits[{i}] is NULL.");
                continue;
            }

            if (unit.popup == null)
            {
                Debug.LogError($"[PopupManager] popupUnits[{i}] ({unit.type}) popup is NULL.");
                continue;
            }

            unit.popup.type = unit.type;

            if (popupDicts.ContainsKey(unit.type))
            {
                Debug.LogError($"[PopupManager] Duplicate popup type: {unit.type}. Check popupUnits list.");
                continue;
            }

            popupDicts.Add(unit.type, unit.popup);
        }
    }

    private void OnDestroy()
    {
        // clear static to avoid stale refs after scene reload
        popupDicts?.Clear();
        popupDicts = null;

        popupData = null;

        activePopups.Clear();
        activeSnapshot.Clear();
    }

    public static void ConfigData(PopupData data) => popupData = data;

    public static void AddEventPopup(PopupType type, BasePopup pop)
    {
        if (pop == null)
        {
            Debug.LogError($"[PopupManager] AddEventPopup failed: pop is null for type {type}");
            return;
        }

        if (popupDicts == null)
        {
            Debug.LogError("[PopupManager] AddEventPopup failed: popupDicts is null (no PopupManager in scene?)");
            return;
        }

        pop.type = type;
        popupDicts[type] = pop;
    }

    public static PopupDataUnit GetPopupData(LevelDifficult state)
    {
        if (popupData == null)
        {
            Debug.LogError("[PopupManager] popupData is null. Call ConfigData() first.");
            return null;
        }
        return popupData.GetPopupData(state);
    }

    public static void OpenPopup(PopupType type)
    {
        if (popupDicts != null && popupDicts.TryGetValue(type, out var popup) && popup != null)
            popup.OpenPopup();
        else
            Debug.LogError($"[PopupManager] OpenPopup failed. Missing popup for type: {type}");
    }

    public static void OpenWitchAction(PopupType type, System.Action action, EnumGameState gameState)
    {
        if (popupDicts != null && popupDicts.TryGetValue(type, out var popup) && popup != null)
            popup.OpenWithAction(action, gameState);
        else
            Debug.LogError($"[PopupManager] OpenWitchAction failed. Missing popup for type: {type}");
    }

    public static bool IsPopupActive(PopupType type) => activePopups.Contains(type);

    public static void SetPopupActive(PopupType type, bool isActive)
    {
        if (isActive) activePopups.Add(type);
        else activePopups.Remove(type);
    }

    public static void CloseAllPopup()
    {
        activeSnapshot.Clear();
        foreach (var t in activePopups) activeSnapshot.Add(t);

        for (int i = 0; i < activeSnapshot.Count; i++)
            ClosePopup(activeSnapshot[i]);

        activeSnapshot.Clear();
    }

    public static void CloseAllExcept(PopupType keepType)
    {
        activeSnapshot.Clear();
        foreach (var t in activePopups)
            if (t != keepType) activeSnapshot.Add(t);

        for (int i = 0; i < activeSnapshot.Count; i++)
            ClosePopup(activeSnapshot[i]);

        activeSnapshot.Clear();
    }

    public static void ClosePopup(PopupType type)
    {
        if (popupDicts != null && popupDicts.TryGetValue(type, out var popup) && popup != null)
        {
            popup.ClosePopup(); // BasePopup will SetPopupActive(false)
        }
        else
        {
            // Ensure active set doesn't get stuck
            activePopups.Remove(type);
            Debug.LogWarning($"[PopupManager] ClosePopup: popup missing for type {type}, removed from active set.");
        }
    }
}


public enum PopupType
{
    Win,
    TimeOut,
    Retry,
    Settings,
    Shop,
    Rate,
    GiveUp,
    NoHeartLeft,
    PurchaseItem,
    UnlockItem,
    MainMenu,
    NoAds,
    PurchaseSuccess,
    PurchaseFail,
    ChangeScene,
    TreasurePopup,
    HeartOffer,
    StarterPack,
    NotEnoughCoin,
    OfferReward,
    LoseStreak,
    EventEnd,
    UnclaimReward,
    RewardEvent,
    VipPass,
    //SpecialOffer,
}
