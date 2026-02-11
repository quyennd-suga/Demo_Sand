using System;
using UnityEngine;

public static class GameDataEvents
{
    public static event Action<int, int> OnCurrencyChanged; // (coins, hearts)
    public static event Action<int> OnCoinsSpent;
    public static event Action<int> OnHeartsSpent;
     public static event Action<int> OnLevelStarted;
    public static event Action<int, int, int> OnLevelCompleted; // (level, stars, score)
    public static event Action<int> OnLevelFailed;
    public static event Action<int> OnLevelUnlocked;
    
    public static event Action<BoosterType> OnBoosterUsed;
    public static event Action<BoosterType, int> OnBoosterPurchased; // (type, quantity)
    public static event Action<BoosterType, int> OnBoosterCountChanged; // (type, newCount)
    
    public static event Action<string> OnAchievementUnlocked;
    
    public static event Action<bool> OnSoundToggled;
    public static event Action<bool> OnMusicToggled;
    public static event Action<bool> OnVibrationToggled;
    

    public static event Action<int, BoosterType?> OnDailyRewardClaimed; // (coins, booster)
    
    #region Trigger Methods
    
    public static void TriggerCurrencyChanged(int coins, int hearts)
    {
        OnCurrencyChanged?.Invoke(coins, hearts);
    }
    
    public static void TriggerCoinsSpent(int amount)
    {
        OnCoinsSpent?.Invoke(amount);
    }
    
    public static void TriggerHeartsSpent(int amount)
    {
        OnHeartsSpent?.Invoke(amount);
    }
    
    public static void TriggerLevelStarted(int level)
    {
        OnLevelStarted?.Invoke(level);
    }
    
    public static void TriggerLevelCompleted(int level, int stars, int score)
    {
        OnLevelCompleted?.Invoke(level, stars, score);
    }
    
    public static void TriggerLevelFailed(int level)
    {
        OnLevelFailed?.Invoke(level);
    }
    
    public static void TriggerLevelUnlocked(int level)
    {
        OnLevelUnlocked?.Invoke(level);
    }
    
    public static void TriggerBoosterUsed(BoosterType type)
    {
        OnBoosterUsed?.Invoke(type);
    }
    
    public static void TriggerBoosterPurchased(BoosterType type, int quantity)
    {
        OnBoosterPurchased?.Invoke(type, quantity);
    }
    
    public static void TriggerBoosterCountChanged(BoosterType type, int newCount)
    {
        OnBoosterCountChanged?.Invoke(type, newCount);
    }
    
    public static void TriggerAchievementUnlocked(string achievementId)
    {
        OnAchievementUnlocked?.Invoke(achievementId);
    }
    
    public static void TriggerSoundToggled(bool enabled)
    {
        OnSoundToggled?.Invoke(enabled);
    }
    
    public static void TriggerMusicToggled(bool enabled)
    {
        OnMusicToggled?.Invoke(enabled);
    }
    
    public static void TriggerVibrationToggled(bool enabled)
    {
        OnVibrationToggled?.Invoke(enabled);
    }
    
    public static void TriggerDailyRewardClaimed(int coins, BoosterType? booster = null)
    {
        OnDailyRewardClaimed?.Invoke(coins, booster);
    }
    
    #endregion
}