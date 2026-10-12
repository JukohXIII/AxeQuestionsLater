using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class UpgradeLevel
{
    public string id;
    public int level;
}

[Serializable]
public class SaveData
{
    public int souls; // TO RENAME WHEN THE NAME IS DEFINED AND LOCKED
    public List<UpgradeLevel> upgrades = new();
}

public class SaveSystem
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "axe_question_later_save.json");

    public static SaveData Load()
    {
        if(!File.Exists(FilePath)) return new SaveData();
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
        }
        catch(Exception e)
        {
            Debug.LogWarning("Save file is unreadable, starting fresh: "+e.Message);
            return new SaveData();
        }
    }

    public static void Save(SaveData data)
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
    }
}
