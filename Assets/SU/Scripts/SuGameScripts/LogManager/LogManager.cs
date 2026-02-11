using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static bool isTestMode;
    public static void Log(object message)
    {
        if(isTestMode)
        {
            Debug.Log(message.ToString());  
        }
    }
}
