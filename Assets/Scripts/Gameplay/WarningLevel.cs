using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningLevel : MonoBehaviour
{
    [SerializeField]
    private GameObject warningHard;
    [SerializeField]
    private GameObject warningSuperHard;


    public void HardWarning()
    {
        gameObject.SetActive(true);
        warningHard.SetActive(true);
        GameController.GameState = EnumGameState.Idle;
        SoundManager.PlaySound(SoundType.HardLevelWarning);
        VibrateHandler.PlayPatternVibrate(Lofelt.NiceVibrations.HapticPatterns.PresetType.Warning);
        //Invoke("HideWarning", 2.1f);
        StartCoroutine(HideWarning());
    }
    public void SuperHardWarning()
    {
        gameObject.SetActive(true);
        warningSuperHard.SetActive(true);
        GameController.GameState = EnumGameState.Idle;
        SoundManager.PlaySound(SoundType.HardLevelWarning);
        VibrateHandler.PlayPatternVibrate(Lofelt.NiceVibrations.HapticPatterns.PresetType.Warning);
        //Invoke("HideWarning", 2.1f);
        StartCoroutine(HideWarning());
    }
    IEnumerator HideWarning()
    {
        yield return new WaitForSeconds(1.8f);
        GameController.GameState = EnumGameState.Playing;
        warningHard.SetActive(false);
        warningSuperHard.SetActive(false);
        gameObject.SetActive(false);
        LevelManager.CheckLevelUnlockItem(); 
    }
}
