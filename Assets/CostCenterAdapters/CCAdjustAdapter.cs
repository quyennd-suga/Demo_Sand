using System;
using System.Collections.Generic;
using UnityEngine;
using AdjustSdk;

namespace CostCenter.Attribution
{
    public class CCAdjustAdapter : CCMMP
    {
        public override void CheckAndGetAttributionID(Action<string> callback)
        {
            base.CheckAndGetAttributionID(callback);
            Adjust.GetAdid(adid =>
            {
                SetAttributionIDToThirdParty(adid);
            });
        }
        private void SetAttributionIDToThirdParty(string attributionID)
        {
            if (string.IsNullOrEmpty(attributionID) == true)
            {
                return;
            }
            Debug.Log($"CCSDK - CCAdjustAdapter: Found an attributionID {attributionID}");
            if (onGetAttributionID != null)
            {
                onGetAttributionID?.Invoke(attributionID);
            }
        }
    }
}
