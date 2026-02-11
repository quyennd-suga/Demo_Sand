using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviour
{
    public static LevelData levelData;

    public static int revive_times;



      
    //private static int cur_lev;
    public static int currentLevel
    {
        get
        {
            return DataManager.data.currentLevel;
        }
        set
        {
            DataManager.data.currentLevel = value;
        }
    }

    
    public static void LoadLevel()
    {
        //testing purpose
        currentLevel = GameController.Instance.currentLevel;


        
        //int levelLoad = CheckLevelSwap();
        //if (levelLoad > GlobalValues.maxLevel)
        //{
        //    int loopDis = GlobalValues.maxLevel - GlobalValues.start_loop_level;
        //    int level = levelLoad - GlobalValues.maxLevel;
        //    levelLoad = level % loopDis + GlobalValues.start_loop_level;
        //}
        
        string path = $"Levels/Level {currentLevel}";
        levelData = Resources.Load<LevelData>(path);
        //SetTimeConfig();


        //ItemManager.onConfigItemByDifficulty?.Invoke();
        CheckLevelUnlockItem();
    }



    //private static int CheckLevelSwap()
    //{
    //    if (GameManager.generalConfig.swap_config == null)
    //        return currentLevel;
    //    for(int i = 0; i < GameManager.generalConfig.swap_config.Length; i++)
    //    {
    //        if(currentLevel == GameManager.generalConfig.swap_config[i].level)
    //        {
    //            if(GameManager.generalConfig.swap_config[i].difficulty_level == GameManager.generalConfig.difficult_level)
    //                return GameManager.generalConfig.swap_config[i].position;
    //        }
    //        if(currentLevel == GameManager.generalConfig.swap_config[i].position)
    //        {
    //            if (GameManager.generalConfig.swap_config[i].difficulty_level == GameManager.generalConfig.difficult_level)
    //                return GameManager.generalConfig.swap_config[i].level;
    //        }
    //    }
    //    return currentLevel;
    //}
    //private static void SetTimeConfig()
    //{
    //    if (GameManager.generalConfig.time_config == null)
    //        return;
    //    for(int i = 0; i < GameManager.generalConfig.time_config.Length; i++)
    //    {
    //        if(currentLevel == GameManager.generalConfig.time_config[i].level)
    //        {
    //            levelData.SaveData.TimeLimit = GameManager.generalConfig.time_config[i].time;
    //            return;
    //        }
    //    }
    //}
    public static void CheckLevelUnlockItem()
    {
        ItemUnlockData[] unlockDatas = DataContainer.Instance.itemData.unlockDatas;
        for (int i = 0; i < unlockDatas.Length; i++)
        {
            Debug.Log("Check unlock for level: " + unlockDatas[i].levelUnlock);
            if (currentLevel == unlockDatas[i].levelUnlock)
            {
                ItemType type = unlockDatas[i].type;
                ItemModule itemModule = DataManager.data.GetItemModule(type);
                if (itemModule.isLock)
                {
                    UnlockItemPopup.itemType = type;
                    UnlockItemPopup.isLevelUnlock = true;
                    //SoundManager.PlaySound(SoundType.PopupEventAppear);
                    PopupManager.OpenPopup(PopupType.UnlockItem);

                }
            }
        }
    }
}
