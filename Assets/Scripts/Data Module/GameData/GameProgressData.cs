using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameProgressData
{
    [Header("Player Progress")]
    public int currentLevel = 1;
    public int maxUnlockedLevel = 1;
    public int totalStarsEarned = 0;
    
    [Header("Currency")]
    public int coins = 0;
    public int hearts = 5;
    public DateTime lastHeartRegenTime;
    
    [Header("Boosters")]
    public Dictionary<BoosterType, int> boosters = new Dictionary<BoosterType, int>();
    
    [Header("Level Completion")]
    public Dictionary<int, LevelCompletionData> levelCompletions = new Dictionary<int, LevelCompletionData>();
    
    [Header("Settings")]
    public bool soundEnabled = true;
    public bool musicEnabled = true;
    public bool vibrationEnabled = true;
    
    [Header("Daily Rewards")]
    public DateTime lastDailyRewardTime;
    public int consecutiveDailyRewards = 0;
    
    public GameProgressData()
    {
        lastHeartRegenTime = DateTime.Now;
        lastDailyRewardTime = DateTime.MinValue;
    }

}

[System.Serializable]
public class LevelCompletionData
{
    public int levelIndex;
    public int stars;
    public int bestScore;
    public bool completed;
    public DateTime completionTime;
    public int attemptsCount;
    
    public LevelCompletionData(int level)
    {
        levelIndex = level;
        stars = 0;
        bestScore = 0;
        completed = false;
        completionTime = DateTime.MinValue;
        attemptsCount = 0;
    }
}

[System.Serializable]
public class BoosterInfo
{
    public BoosterType type;
    public string name;
    public string description;
    public int coinCost;
    public Sprite icon;
    
    public BoosterInfo(BoosterType boosterType, string boosterName, string desc, int cost)
    {
        type = boosterType;
        name = boosterName;
        description = desc;
        coinCost = cost;
    }
}