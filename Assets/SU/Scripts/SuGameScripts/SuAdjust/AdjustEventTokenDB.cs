//using Sirenix.OdinInspector;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "AdjusEventTokenDB", menuName = "DB/AdjustEventTokenDB")]
public class AdjustEventTokenDB : ScriptableObject
{
    public List<AdjustEventTokenModule> EventTokens;
    public Dictionary<EventName, string> EventTokenDict;
    private void OnEnable()
    {
        EventTokenDict = new Dictionary<EventName, string>();
        foreach(AdjustEventTokenModule md in EventTokens)
        {
            if(!EventTokenDict.ContainsKey(md.eventName))
            {
                EventTokenDict.Add(md.eventName, md.token);
            }
        }
    }

    public string GetToken(string eventName)
    {
        bool parse = System.Enum.TryParse<EventName>(eventName, out EventName evName);
        if (parse)
        {
            if (EventTokenDict.ContainsKey(evName))
            {
                return EventTokenDict[evName];
            }
            else
            {
                return "";
            }
        }
        else
        {
            return "";
        }
    }

    public string GetToken(EventName eventName)
    {
        if (EventTokenDict.ContainsKey(eventName))
        {
            return EventTokenDict[eventName];
        }
        else
        { 
            return "";
        } 
    }
}  

[System.Serializable]
public class AdjustEventTokenModule
{
    [EnumPaging]
    public EventName eventName;
    public string token;
}