using UnityEngine;
using System.Collections;

    public abstract class BaseSUUnit : MonoBehaviour
    {
        [SerializeField]
        protected bool enableSU = true;
        public bool EnableSU
        {
            get
            {
                return enableSU;
            }
        }
        public abstract void Init(bool isTestMode);
    [HideInInspector]
    public bool isTest;
    }
