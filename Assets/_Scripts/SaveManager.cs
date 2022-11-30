using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SaveManager
{
    public const string SAVELOCATION = "/save.json";

    private static string getStorageLocation => Application.streamingAssetsPath + SAVELOCATION;



    public static void WriteSave(SaveDataContainer data)
    {
        RemoveSave();
        File.Create(getStorageLocation).Dispose();

        using (StreamWriter w = new StreamWriter(getStorageLocation))
        {
            w.Write(JsonUtility.ToJson(data, true));
        }

        DebugMessage("Game Saved");
    }

    public static bool ReadData(out SaveDataContainer data)
    {
        if (SaveExists())
        {
            using (StreamReader r = new StreamReader(getStorageLocation))
            {
                data = JsonUtility.FromJson<SaveDataContainer>(r.ReadToEnd());
            }

            DebugMessage("Load Completed");
            return true;
        }


        DebugMessage("Load Failed");
        data = new SaveDataContainer();
        return false;
    }


    public static void RemoveSave()
    {
        if (SaveExists()) File.Delete(getStorageLocation);
        DebugMessage("Save Removed");
    }

    public static bool SaveExists() => File.Exists(getStorageLocation);

    private static void DebugMessage(string msg)
    {
#if UNITY_EDITOR
        Debug.Log(msg);
#endif
    }

}


public struct SaveDataContainer
{
    public float totalTime;
    public int pushCount;
    public int pointCount;

    public Vector2Int mapSize;

    public SavedData_SavedPresence[] savedMap;


    public SaveDataContainer(MapPresence[,] source, float totalTime, int pushCount, int pointCount)
    {
        this.totalTime = totalTime;
        this.pushCount = pushCount;
        this.pointCount = pointCount;
        this.mapSize = new Vector2Int(source.GetLength(0), source.GetLength(1));


        List<SavedData_SavedPresence> saved = new List<SavedData_SavedPresence>();

        for(int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (source[x, y])
                    saved.Add(new SavedData_SavedPresence(source[x, y]));
            }
        }

        savedMap = saved.ToArray();
    }
}


[System.Serializable]
public  class SavedData_SavedPresence
{
    public Vector2Int pos;
    public int type;
    public bool hasPlayerOn;

    public bool isValid => pos != Vector2.one * -1;

    public SavedData_SavedPresence(MapPresence presence)
    {
        if (presence)
        {
            pos = presence.getPos;
            type = (int)presence.getType;
            hasPlayerOn = presence.getHarbouringPlayer;
        }
        else
            pos = new Vector2Int(-1, -1);
    }
}