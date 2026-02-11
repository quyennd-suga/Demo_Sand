using System;
using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : Singleton<GameDataManager>
{
    [Header("Game Settings")]
    [SerializeField] private int maxHearts = 5;
    [SerializeField] private float heartRegenTimeMinutes = 30f;
    [SerializeField] private int coinsPerLevel = 50;
    
    [Header("Booster Settings")]
    [SerializeField] private List<BoosterInfo> boosterInfos = new List<BoosterInfo>();
    
    private GameProgressData gameData;
    private const string SAVE_KEY = "GameProgressData";
    
    // Events
    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnHeartsChanged;
    public static event Action<int> OnLevelCompleted;
    public static event Action<BoosterType, int> OnBoosterChanged;
    
    protected override void Awake()
    {
        base.Awake();
        LoadGameData();
    }
    
    private void Start()
    {
        InvokeRepeating(nameof(RegenerateHearts), 1f, 1f); 
    }
    
    #region Data Management
    
    private void LoadGameData()
    {
        string jsonData = PlayerPrefs.GetString(SAVE_KEY, "");
        
        if (string.IsNullOrEmpty(jsonData))
        {
            gameData = new GameProgressData();
            SaveGameData();
        }
        else
        {
            try
            {
                gameData = JsonUtility.FromJson<GameProgressData>(jsonData);
                if (gameData == null)
                {
                    gameData = new GameProgressData();
                }
            }
            catch (Exception e)
            {
                gameData = new GameProgressData();
            }
        }
    }
    
    public void SaveGameData()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(gameData, true);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
        }
    }
    
    public void ResetGameData()
    {
        gameData = new GameProgressData();
        SaveGameData();
        
        // Trigger events
        OnCoinsChanged?.Invoke(gameData.coins);
        OnHeartsChanged?.Invoke(gameData.hearts);
    }
    
    #endregion
    
    #region Level Management
    
    public int GetCurrentLevel() => gameData.currentLevel;
    public int GetMaxUnlockedLevel() => gameData.maxUnlockedLevel;
    public int GetTotalStars() => gameData.totalStarsEarned;
    
    public bool IsLevelUnlocked(int level)
    {
        return level <= gameData.maxUnlockedLevel;
    }
    
    public LevelCompletionData GetLevelCompletion(int level)
    {
        if (gameData.levelCompletions.TryGetValue(level, out LevelCompletionData completion))
        {
            return completion;
        }
        return new LevelCompletionData(level);
    }
    
    public void CompleteLevel(int level, int stars, int score)
    {
        if (!gameData.levelCompletions.ContainsKey(level))
        {
            gameData.levelCompletions[level] = new LevelCompletionData(level);
        }
        
        var completion = gameData.levelCompletions[level];
        bool isFirstCompletion = !completion.completed;
        
        completion.completed = true;
        completion.completionTime = DateTime.Now;
        
        // Update best score and stars
        if (score > completion.bestScore)
        {
            completion.bestScore = score;
        }
        
        if (stars > completion.stars)
        {
            int starDifference = stars - completion.stars;
            completion.stars = stars;
            gameData.totalStarsEarned += starDifference;
        }
        
        // Unlock next level
        if (level >= gameData.maxUnlockedLevel)
        {
            gameData.maxUnlockedLevel = level + 1;
        }
        
        // Award coins for completion
        if (isFirstCompletion)
        {
            AddCoins(coinsPerLevel);
        }
        
        SaveGameData();
        OnLevelCompleted?.Invoke(level);
    }
    
    public void SetCurrentLevel(int level)
    {
        if (IsLevelUnlocked(level))
        {
            gameData.currentLevel = level;
            SaveGameData();
        }
    }
    
    #endregion
    
    #region Currency Management
    
    public int GetCoins() => gameData.coins;
    public int GetHearts() => gameData.hearts;
    
    public bool CanSpendCoins(int amount)
    {
        return gameData.coins >= amount;
    }
    
    public bool CanSpendHearts(int amount)
    {
        return gameData.hearts >= amount;
    }
    
    public void AddCoins(int amount)
    {
        gameData.coins += amount;
        SaveGameData();
        OnCoinsChanged?.Invoke(gameData.coins);
    }
    
    public bool SpendCoins(int amount)
    {
        if (CanSpendCoins(amount))
        {
            gameData.coins -= amount;
            SaveGameData();
            OnCoinsChanged?.Invoke(gameData.coins);
            return true;
        }
        return false;
    }
    
    public void AddHearts(int amount)
    {
        gameData.hearts = Mathf.Min(gameData.hearts + amount, maxHearts);
        SaveGameData();
        OnHeartsChanged?.Invoke(gameData.hearts);
    }
    
    public bool SpendHearts(int amount)
    {
        if (CanSpendHearts(amount))
        {
            gameData.hearts -= amount;
            gameData.lastHeartRegenTime = DateTime.Now;
            SaveGameData();
            OnHeartsChanged?.Invoke(gameData.hearts);
            return true;
        }
        return false;
    }
    
    private void RegenerateHearts()
    {
        if (gameData.hearts >= maxHearts) return;
        if (heartRegenTimeMinutes <= 0) return; 
        
        DateTime now = DateTime.Now;
        TimeSpan timeSinceLastRegen = now - gameData.lastHeartRegenTime;
        
        double totalMinutesPassed = timeSinceLastRegen.TotalMinutes;
        int heartsToAdd = (int)(totalMinutesPassed / heartRegenTimeMinutes);
        
        if (heartsToAdd > 0)
        {
            int newHeartCount = Mathf.Min(gameData.hearts + heartsToAdd, maxHearts);
            int actualHeartsAdded = newHeartCount - gameData.hearts;
            
            if (actualHeartsAdded > 0)
            {
                gameData.hearts = newHeartCount;
                
                gameData.lastHeartRegenTime = gameData.lastHeartRegenTime.AddMinutes(actualHeartsAdded * heartRegenTimeMinutes);
                
                SaveGameData();
                OnHeartsChanged?.Invoke(gameData.hearts);
               
            }
        }
    }
    
    public TimeSpan GetTimeUntilNextHeart()
    {
        if (gameData.hearts >= maxHearts)
            return TimeSpan.Zero;
        
        DateTime nextHeartTime = gameData.lastHeartRegenTime.AddMinutes(heartRegenTimeMinutes);
        TimeSpan timeUntilNext = nextHeartTime - DateTime.Now;
        
        return timeUntilNext > TimeSpan.Zero ? timeUntilNext : TimeSpan.Zero;
    }
    
    public void SetInfiniteHearts(bool infinite)
    {
        if (infinite)
        {
            gameData.hearts = maxHearts; 
        }
        
        OnHeartsChanged?.Invoke(gameData.hearts);
    }
    
    public void SetHeartRegenTime(float minutes)
    {
        heartRegenTimeMinutes = Mathf.Max(0.1f, minutes); // Minimum 0.1 minute to avoid issues
    }
    
    public void SetHeartRegenTimeSeconds(int seconds)
    {
        heartRegenTimeMinutes = Mathf.Max(0.1f, seconds / 60f); // Convert to minutes
    }
    
    public void ForceRegenerateHearts()
    {
        if (gameData.hearts < maxHearts)
        {
            gameData.hearts++;
            gameData.lastHeartRegenTime = DateTime.Now;
            SaveGameData();
            OnHeartsChanged?.Invoke(gameData.hearts);
        }
    }
    
    public string GetHeartRegenDebugInfo()
    {
        DateTime now = DateTime.Now;
        TimeSpan timeSinceLastRegen = now - gameData.lastHeartRegenTime;
        TimeSpan timeUntilNext = GetTimeUntilNextHeart();
        
        return $"Hearts: {gameData.hearts}/{maxHearts}\n" +
               $"Last Regen: {gameData.lastHeartRegenTime:HH:mm:ss}\n" +
               $"Current Time: {now:HH:mm:ss}\n" +
               $"Time Since Last: {timeSinceLastRegen.TotalMinutes:F1} min\n" +
               $"Time Until Next: {timeUntilNext.TotalMinutes:F1} min\n" +
               $"Regen Interval: {heartRegenTimeMinutes:F2} min";
    }
    
    public void QuickTestHeartRegen()
    {
        SpendHearts(1);
        SetHeartRegenTimeSeconds(30);
    }
    
    #endregion
    
    #region Booster Management
    
    
    public int GetBoosterCount(BoosterType type)
    {
        return gameData.boosters.TryGetValue(type, out int count) ? count : 0;
    }
    
    public bool CanUseBooster(BoosterType type)
    {
        return GetBoosterCount(type) > 0;
    }
    
    public void AddBooster(BoosterType type, int amount)
    {
        if (!gameData.boosters.ContainsKey(type))
        {
            gameData.boosters[type] = 0;
        }
        
        gameData.boosters[type] += amount;
        SaveGameData();
        OnBoosterChanged?.Invoke(type, gameData.boosters[type]);
    }
    
    public bool UseBooster(BoosterType type)
    {
        if (CanUseBooster(type))
        {
            gameData.boosters[type]--;
            SaveGameData();
            OnBoosterChanged?.Invoke(type, gameData.boosters[type]);
            return true;
        }
        return false;
    }
    
    public bool BuyBooster(BoosterType type, int quantity = 1)
    {
        BoosterInfo info = GetBoosterInfo(type);
        if (info == null) return false;
        
        int totalCost = info.coinCost * quantity;
        if (SpendCoins(totalCost))
        {
            AddBooster(type, quantity);
            return true;
        }
        return false;
    }
    
    public BoosterInfo GetBoosterInfo(BoosterType type)
    {
        return boosterInfos.Find(b => b.type == type);
    }
    
    public List<BoosterInfo> GetAllBoosterInfos()
    {
        return new List<BoosterInfo>(boosterInfos);
    }
    
    #endregion
    
    #region Settings
    
    public bool IsSoundEnabled() => gameData.soundEnabled;
    public bool IsMusicEnabled() => gameData.musicEnabled;
    public bool IsVibrationEnabled() => gameData.vibrationEnabled;
    
    public void SetSoundEnabled(bool enabled)
    {
        gameData.soundEnabled = enabled;
        SaveGameData();
    }
    
    public void SetMusicEnabled(bool enabled)
    {
        gameData.musicEnabled = enabled;
        SaveGameData();
    }
    
    public void SetVibrationEnabled(bool enabled)
    {
        gameData.vibrationEnabled = enabled;
        SaveGameData();
    }
    
    #endregion
    

}