using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "ScriptableObjects/FruitData", order = 1)]
public class FruitData : ScriptableObject
{
    public List<FruitInfo> fruits;

    public Dictionary<ColorEnum, Sprite> fruitSpriteMap;

    private void OnEnable()
    {
        fruitSpriteMap = new Dictionary<ColorEnum, Sprite>();
        foreach (var fruit in fruits)
        {
            fruitSpriteMap[fruit.fruitColor] = fruit.fruitSprite;
        }
    }
    private void OnValidate()
    {
        fruitSpriteMap = new Dictionary<ColorEnum, Sprite>();
        foreach (var fruit in fruits)
        {
            fruitSpriteMap[fruit.fruitColor] = fruit.fruitSprite;
        }
    }


    public Sprite GetFruitSprite(ColorEnum color)
    {
        if (fruitSpriteMap != null && fruitSpriteMap.TryGetValue(color, out var sprite))
        {
            return sprite;
        }
        Debug.LogWarning($"Fruit sprite for color {color} not found. Returning null.", this);
        return null;
    }    
}

[System.Serializable]
public struct FruitInfo
{
    public ColorEnum fruitColor;
    public Sprite fruitSprite;
}
