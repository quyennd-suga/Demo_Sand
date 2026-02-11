using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lofelt.NiceVibrations;
using System.Security;

public class VibrateHandler : MonoBehaviour
{
    public static bool isVibrate
    {
        get
        {
            return DataManager.data.vibrate;
        }
        set
        {
            DataManager.data.vibrate = value;

        }
    }
    public static void Vibrate()
    {
        if (isVibrate)
        {
#if UNITY_ANDROID
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(0.471f, 0.0f, 0.04f);
                //HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
            }
#else
            if(DeviceCapabilities.isVersionSupported)
            {
                //HapticPatterns.PlayConstant(0.5f, 0.0f, 0.1f);
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
            }    
#endif
        }

    }
    public static void ButtonVibrate()
    {
        if (isVibrate)
        {
#if UNITY_ANDROID
            if (DeviceCapabilities.isVersionSupported)
            {
                //HapticPatterns.PlayConstant(0.5f, 0.0f, 0.1f);
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
            }
#else
            if(DeviceCapabilities.isVersionSupported)
            {
                //HapticPatterns.PlayConstant(0.5f, 0.0f, 0.1f);
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
            }    
#endif
        }
    }

    public static void CollectItems()
    {
        if (isVibrate)
        {
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(0.1f, 0f, 0.1f);
            }
        }
    }
    public static void LightVibrate()
    {
        if (isVibrate)
        {
#if UNITY_ANDROID
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(0.01f, 0f, 0.01f);
            }
#else
            if(DeviceCapabilities.isVersionSupported)
            {
                //HapticPatterns.PlayConstant(0.5f, 0.0f, 0.1f);
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
            }    
#endif
        }
    }


    public static void PlayPatternVibrate(HapticPatterns.PresetType hapticPatterns)
    {
        if (isVibrate)
        {
            if (DeviceCapabilities.isVersionSupported)
                HapticPatterns.PlayPreset(hapticPatterns);
        }
    }    


    public static void ShuffleVibrate()
    {
        if (isVibrate)
        {
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(0.08f, 0f, 1f);
            }
        }
    }


    public static void CutVibrate()
    {
        if (isVibrate)
        {
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(1f, 0f, 0.15f);
            }
        }
    }

    public static void CollectRope()
    {
        if (isVibrate)
        {
            if (DeviceCapabilities.isVersionSupported)
            {
                HapticPatterns.PlayConstant(0.471f, 0f, 0.11f);
            }
        }
    }
}
