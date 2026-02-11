//using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
public class SoundManager : MonoBehaviour
{
    private static List<SourceUnit> sourceUnits = new List<SourceUnit>();
    [SerializeField]
    private SoundUnit[] soundUnits;
    private static Dictionary<SoundType, SoundData> soundDict;


    public static bool sound
    {
        get
        {
            return DataManager.data.sound;
        }
        set
        {
            DataManager.data.sound = value;
            foreach (var source in sourceUnits)
            {
                if (source.type == SourceType.Sound)
                {
                    source.audioSource.enabled = value;
                }
            }
            
        }
    }

    public static bool music
    {
        get
        {
            return DataManager.data.music;
        }
        set
        {
            DataManager.data.music = value;
            foreach (var source in sourceUnits)
            {
                if (source.type == SourceType.Music)
                {
                    source.audioSource.enabled = value;
                    if (value)
                    {
                        if(source.audioSource.isPlaying == false)
                            source.audioSource.Play();
                    }
                    else
                    {
                        source.audioSource.Stop();
                    }
                }
            }
        }
    }
 

    private void Start()
    {
        soundDict = new Dictionary<SoundType, SoundData>();
        for(int i = 0; i < soundUnits.Length; i++)
        {
            soundDict.Add(soundUnits[i].type, new SoundData(soundUnits[i].soundClip, soundUnits[i].sourceType, soundUnits[i].volume));
        }
        sourceUnits = GetComponentsInChildren<SourceUnit>().ToList();
        music = DataManager.data.music;
        foreach (var source in sourceUnits)
        {
            if (source.type == SourceType.Music)
            {
                source.audioSource.loop = true;
                source.audioSource.clip = soundDict[SoundType.HomeMusic].audioClip;
                source.audioSource.volume = soundDict[SoundType.HomeMusic].volume;
                if (music)
                {
                    FadeInMusic(1f);
                    source.audioSource.enabled = true;
                    source.audioSource.Play();
                }
            }
        }
    }
    public static void ChangeMusicTrack(SoundType soundType)
    {
        foreach (var source in sourceUnits)
        {
            if (source.type == SourceType.Music)
            {
                source.audioSource.clip = soundDict[soundType].audioClip;
                source.audioSource.volume = soundDict[soundType].volume;
                source.audioSource.Play();
            }
        }
    } 
    
    public static void FadeOutMusic(float duration)
    {
        foreach (var source in sourceUnits)
        {
            if (source.type == SourceType.Music)
            {
                source.audioSource.DOFade(0, duration);
            }
        }
    }

    public static void FadeInMusic(float duration)
    {
        float volume = soundDict[SoundType.HomeMusic].volume;
        if(GameController.GameState != EnumGameState.Home)
        {
            volume = soundDict[SoundType.GameplayMusic].volume;
        }
        foreach (var source in sourceUnits)
        {
            if (source.type == SourceType.Music)
            {
                source.audioSource.DOFade(volume, duration);
            }
        }
    }
    public static void PlaySound(SoundType soundType)
    {
        if (!sound)
            return;

        if(!soundDict.ContainsKey(soundType))
        {
            Debug.LogWarning("SoundType " + soundType.ToString() + " not found in soundDict!");
            return;
        }
        SourceType sourceType = soundDict[soundType].audioSource;
        if(sourceType != SourceType.Sound)
        {
            return;
        }    
        int length = sourceUnits.Count;
        for(int i = 0; i < length; i++)
        {
            if (sourceUnits[i].audioSource.isPlaying == false)
            {
                if (sourceUnits[i].type == sourceType)
                {
                    float volume = soundDict[soundType].volume;
                    sourceUnits[i].audioSource.PlayOneShot(soundDict[soundType].audioClip, volume);
                    return;
                }    
                
            } 
        }
        sourceUnits[0].audioSource.PlayOneShot(soundDict[soundType].audioClip, soundDict[soundType].volume);
    }
    
    
}


public enum SoundType
{
    PickPin,
    CollectRope,
    ScissorsCut,
    FreezeTime,
    ClickButton,
    CoinCollect,
    LevelComplete,
    CoinAppear,
    Shuffle,
    TimeOut,
    TimeWarning,
    TenSecLeft,
    SwitchScene,
    ClaimReward,
    Confetti,
    SingleConfetti,
    PlayButton,
    SwitchTab,
    HardLevelWarning,
    PopupEventAppear,
    UnlockItem,
    CutMove,
    GameplayMusic,
    HomeMusic,
}

[System.Serializable]
public class SoundUnit
{
    //[EnumPaging]
    public SoundType type;

    //[EnumPaging]
    public SourceType sourceType;

    public AudioClip soundClip;

    public float volume;
    
}

public enum SourceType
{
    Music,
    Sound,

}


[System.Serializable]   
public class SoundData
{
    public AudioClip audioClip;
    public SourceType audioSource;
    public float volume = 0.7f;

    public SoundData(AudioClip audioClip, SourceType audioSource, float vol)
    {
        this.audioClip = audioClip;
        this.audioSource = audioSource;
        this.volume = vol;
    }
}
