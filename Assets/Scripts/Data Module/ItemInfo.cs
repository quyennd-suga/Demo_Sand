using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ItemInfo
{
    public ItemType type;
    public int value;
}

public enum ItemType
{
    Freeze,
    Pump,
    Expand,
    Hammer,
    Coin,
    Heart,
   
}
