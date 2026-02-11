using UnityEngine;

public class GameDataExample : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 60;
    }
    private void Start()
    {
        GameDataManager.OnCoinsChanged += OnCoinsChanged;
        GameDataManager.OnHeartsChanged += OnHeartsChanged;
        GameDataManager.OnLevelCompleted += OnLevelCompleted;
        GameDataManager.OnBoosterChanged += OnBoosterChanged;
        
        TestGameDataManager();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe events
        if (GameDataManager.Instance != null)
        {
            GameDataManager.OnCoinsChanged -= OnCoinsChanged;
            GameDataManager.OnHeartsChanged -= OnHeartsChanged;
            GameDataManager.OnLevelCompleted -= OnLevelCompleted;
            GameDataManager.OnBoosterChanged -= OnBoosterChanged;
        }
    }
    
    private void TestGameDataManager()
    {
        var dataManager = GameDataManager.Instance;
        
        Debug.Log("=== GAME DATA MANAGER TEST ===");
        
        // Test currency
        Debug.Log($"Current Coins: {dataManager.GetCoins()}");
        Debug.Log($"Current Hearts: {dataManager.GetHearts()}");
        
        // Test level progress
        Debug.Log($"Current Level: {dataManager.GetCurrentLevel()}");
        Debug.Log($"Max Unlocked Level: {dataManager.GetMaxUnlockedLevel()}");
        Debug.Log($"Total Stars: {dataManager.GetTotalStars()}");
        
        // Test boosters
        foreach (BoosterType boosterType in System.Enum.GetValues(typeof(BoosterType)))
        {
            int count = dataManager.GetBoosterCount(boosterType);
            Debug.Log($"Booster {boosterType}: {count}");
        }
    }
    
    #region Example Usage Methods
    
    [ContextMenu("Add 100 Coins")]
    public void AddCoins()
    {
        GameDataManager.Instance.AddCoins(100);
    }
    
    [ContextMenu("Spend 50 Coins")]
    public void SpendCoins()
    {
        bool success = GameDataManager.Instance.SpendCoins(50);
        Debug.Log($"Spend coins result: {success}");
    }
    
    [ContextMenu("Spend 1 Heart")]
    public void SpendHeart()
    {
        bool success = GameDataManager.Instance.SpendHearts(1);
        Debug.Log($"Spend heart result: {success}");
    }
    
    [ContextMenu("Complete Current Level")]
    public void CompleteLevel()
    {
        int currentLevel = GameDataManager.Instance.GetCurrentLevel();
        int stars = Random.Range(1, 4); // 1-3 stars
        int score = Random.Range(1000, 5000);
        
        GameDataManager.Instance.CompleteLevel(currentLevel, stars, score);
        Debug.Log($"Completed level {currentLevel} with {stars} stars and {score} score");
    }
    
   
    [ContextMenu("Reset Game Data")]
    public void ResetGameData()
    {
        GameDataManager.Instance.ResetGameData();
        Debug.Log("Game data reset!");
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnCoinsChanged(int newAmount)
    {
        Debug.Log($"💰 Coins changed to: {newAmount}");
    }
    
    private void OnHeartsChanged(int newAmount)
    {
        Debug.Log($"❤️ Hearts changed to: {newAmount}");
    }
    
    private void OnLevelCompleted(int level)
    {
        Debug.Log($"🎉 Level {level} completed!");
    }
    
    private void OnBoosterChanged(BoosterType type, int newCount)
    {
        Debug.Log($"🚀 Booster {type} count changed to: {newCount}");
    }
    
    #endregion
    
    #region UI Helper Methods (for UI scripts)
    
    /// <summary>
    /// Lấy thông tin hiển thị currency cho UI
    /// </summary>
    public static string GetCurrencyDisplayText()
    {
        var dataManager = GameDataManager.Instance;
        return $"💰 {dataManager.GetCoins()}  ❤️ {dataManager.GetHearts()}";
    }
    
    /// <summary>
    /// Lấy thông tin thời gian regenerate heart
    /// </summary>
    public static string GetHeartRegenTimeText()
    {
        var timeUntilNext = GameDataManager.Instance.GetTimeUntilNextHeart();
        if (timeUntilNext == System.TimeSpan.Zero)
            return "Full";
        
        return $"{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
    }
    
    /// <summary>
    /// Kiểm tra có thể chơi level không (có đủ heart)
    /// </summary>
    public static bool CanPlayLevel()
    {
        return GameDataManager.Instance.CanSpendHearts(1);
    }
    
    /// <summary>
    /// Lấy thông tin level completion để hiển thị stars
    /// </summary>
    public static LevelCompletionData GetLevelDisplayInfo(int level)
    {
        return GameDataManager.Instance.GetLevelCompletion(level);
    }
    
    #endregion
}