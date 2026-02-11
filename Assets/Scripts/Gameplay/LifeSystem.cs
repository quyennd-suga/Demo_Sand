
using System;

public class LifeSystem
{
    public static bool isFreezeTime;
    public static int freezeTime
    {
        get
        {
            return DataManager.data.freezeTime;
        }
        set
        {
            DataManager.data.freezeTime = value;
        }
    }
    public static Action<int> onLifeChange;
    public static Action onFreezeTime;
    private static int _live;
    public static int totalLives
    {
        get
        {
            if (isFreezeTime)
                return 5;
            return _live;
        }
        set
        {
            if (isFreezeTime)
                return;
            
            if(value < 0)
            {
                value = 0;
            }
            if(value > 5)
            {
                value = 5;
            }
            onLifeChange?.Invoke(value);
            _live = value;
            DataManager.data.lives = value;
        }
    }
}
