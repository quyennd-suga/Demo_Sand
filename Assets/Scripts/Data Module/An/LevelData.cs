using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Level", menuName = "Level Generator/Create Level")]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;

    public int levelIndex;

    public LevelDifficult levelDifficult;
    public int time;
    public CameraData cameraData = new CameraData();

    public List<NodeData> emptyNodes = new List<NodeData>();
    public List<PipeData> pipes = new List<PipeData>();
    public List<BlockData> blocks = new List<BlockData>();
    public List<ColorPathData> colorPaths = new List<ColorPathData>();
}

public enum LevelDifficult
{
    Normal,
    Hard,
    SuperHard
}
[System.Serializable]
public class CameraData
{
    public float positionX;
    public float positionY;
    public float positionZ = -10f;
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float fov = 60f;

    public CameraData() { }

    public CameraData(CameraData other)
    {
        positionX = other.positionX;
        positionY = other.positionY;
        positionZ = other.positionZ;
        rotationX = other.rotationX;
        rotationY = other.rotationY;
        rotationZ = other.rotationZ;
        fov = other.fov;
    }
}