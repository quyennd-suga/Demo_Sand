using Firebase.Crashlytics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace SU
{
    public class SuCrashlytics : BaseSUUnit
    {

        int AndroidAPI
        {
            get
            {
                int apiLevel = 26;
                int.TryParse(SystemInfo.operatingSystem.Substring(SystemInfo.operatingSystem.IndexOf("-") + 1, 3), out apiLevel);
                Debug.Log("PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP AndroidAPI Level la : " + apiLevel);
                return apiLevel;
            }
        }

        public override void Init(bool isTest)
        {
            //Crashlytics.IsCrashlyticsCollectionEnabled = true;
        }


        public void ExceptionTest()
        {
            throw new System.Exception("test exception please ignore");
        }

        public void CrashTest()
        {
            //Application.ForceCrash(0);
        }




        Queue<string> LogQueue;
        private void Awake()
        {
            if (LogQueue == null)
            {
                LogQueue = new Queue<string>();
            }
            Application.logMessageReceived += HandleLog;
        }
        void HandleLog(string logString, string stackTrace, LogType type)
        {
                        
        }



    }
}
