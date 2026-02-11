using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class Switcher : MonoBehaviour
{
    [SerializeField] private bool isOn;
    [SerializeField] private SettingType settingType;
    [SerializeField] private Image switchImage;
    [SerializeField] private GameObject x;
    public Sprite onSprite;
    public Sprite offSprite;

    private void SetupData()
    {
        switch (settingType)
        {
            case SettingType.BackgroundSound:
                isOn = true;// GameManager.dataSave.isMusicOn;
                break;
            case SettingType.FxSound:
                isOn = true;// GameManager.dataSave.isSoundOn;
                break;
            case SettingType.Vibration:
                isOn = true;// GameManager.dataSave.isVibrate;
                break;
        }
    }

    private void SetupUI()
    {
        switchImage.sprite = isOn ? onSprite : offSprite;
        x.SetActive(!isOn);
    }

    private void OnEnable()
    {
        SetupData();
        SetupUI();
    }

    public void Switching()
    {
      
        switch (settingType)
        {
            case SettingType.BackgroundSound:
               
                break;
            case SettingType.FxSound:
                break;
            case SettingType.Vibration:
                break;
        }


        isOn = !isOn;

        //SoundManager.Ins.PlaySound(AudioClipType.SFX_tapButton);
        //isOn = !isOn;
        // SoundManager.Ins.PlaySound(AudioClipType.SFX_tapButton);
        SetupData();
        SetupUI();


    }
}

public enum SettingType
{
    BackgroundSound,
    FxSound,
    Vibration,
}

public enum SwitchState
{
    Idle,
    Moving,
}