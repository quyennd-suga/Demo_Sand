using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game Data/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Currency Settings")]
    [SerializeField] private int maxHearts = 5;
    [SerializeField] private float heartRegenTimeMinutes = 30f;
    [SerializeField] private int coinsPerLevel = 50;
    [SerializeField] private int coinsPerStar = 10;
    
    [Header("Level Settings")]
    [SerializeField] private int heartsPerLevel = 1;
    [SerializeField] private int starsForPerfect = 3;
    
    [Header("Booster Settings")]
    [SerializeField] private List<BoosterConfigData> boosterConfigs = new List<BoosterConfigData>();
    
    [Header("Daily Reward Settings")]
    [SerializeField] private List<DailyRewardData> dailyRewards = new List<DailyRewardData>();
    
    [Header("Achievement Settings")]
    [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();
    
    // Properties
    public int MaxHearts => maxHearts;
    public float HeartRegenTimeMinutes => heartRegenTimeMinutes;
    public int CoinsPerLevel => coinsPerLevel;
    public int CoinsPerStar => coinsPerStar;
    public int HeartsPerLevel => heartsPerLevel;
    public int StarsForPerfect => starsForPerfect;
    
    public List<BoosterConfigData> BoosterConfigs => boosterConfigs;
    public List<DailyRewardData> DailyRewards => dailyRewards;
    public List<AchievementData> Achievements => achievements;
    
    private void OnValidate()
    {

    }
    

    
    public BoosterConfigData GetBoosterConfig(BoosterType type)
    {
        return boosterConfigs.Find(config => config.type == type);
    }
    
    public DailyRewardData GetDailyReward(int day)
    {
        int index = (day - 1) % dailyRewards.Count;
        return dailyRewards[index];
    }
}

[System.Serializable]
public class BoosterConfigData
{
    public BoosterType type;
    public string name;
    [TextArea(2, 3)]
    public string description;
    public int coinCost;
    public int startingAmount;
    public Sprite icon;
}

[System.Serializable]
public class DailyRewardData
{
    public int day;
    public int coins;
    public BoosterType boosterType;
    public int boosterAmount;
}

[System.Serializable]
public class AchievementData
{
    public string id;
    public string name;
    [TextArea(2, 3)]
    public string description;
    public AchievementType type;
    public int targetValue;
    public int coinReward;
    public BoosterType boosterReward;
    public int boosterAmount;
    public Sprite icon;
}

public enum AchievementType
{
    CompleteLevel,    
    EarnStars,         
    SpendCoins,       
    UseBooster,       
    ConsecutiveDays    
}