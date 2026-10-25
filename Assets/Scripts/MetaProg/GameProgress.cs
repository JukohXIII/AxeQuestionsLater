using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class UpgradeLevel
{
    public string id;
    public int level;
}

[Serializable]
public class SaveData
{
    public int souls;
    public List<UpgradeLevel> upgrades = new ();
}

public class GameProgress
{
    private static SaveData data;
    private static bool dirty;
    public static event Action SoulsChanged;
    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        data = null;
        dirty = false;
        SoulsChanged = null;
        Application.quitting -= Save; // no double subscription when domain reload is off
        Application.quitting += Save;
    }

    private static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    public static int Souls => Data.souls;

    public static void AddSouls(int amount)
    {
        if (amount <= 0) return;
        Data.souls += amount;
        dirty = true;
        SoulsChanged?.Invoke();
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Data.souls < amount) return false;
        Data.souls -= amount;
        dirty = true;
        SoulsChanged?.Invoke();
        return true;
    }

    public static int GetLevel(string id)
    {
        foreach (var upgrade in Data.upgrades)
            if (upgrade.id == id) return upgrade.level;
        return 0;
    }

    public static void SetLevel(string id, int level)
    {
        dirty = true;
        foreach (var upgrade in Data.upgrades)
        {
            if (upgrade.id != id) continue;
            upgrade.level = level;
            return;
        }
        Data.upgrades.Add(new UpgradeLevel { id = id, level = level});
    }

    private static void Load()
    {
        data = new SaveData();
        Debug.Log($"[Save] {FilePath}");

        if (!File.Exists(FilePath)) return;
        try
        {
            var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (loaded != null) data = loaded;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Save unreadable, starting fresh: {e.Message}");
            File.Copy(FilePath, FilePath + ".corrupt", true);
        }
    }

    public static void Save()
    {
        if(!dirty) return;
        try
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
            File.Copy(tmp, FilePath, true);
            File.Delete(tmp);
            dirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }
}
