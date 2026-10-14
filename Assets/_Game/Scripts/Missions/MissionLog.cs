using System;
using System.IO;
using UnityEngine;

public sealed class MissionLog
{
    [Serializable]
    private sealed class SaveData
    {
        public int Version = 1;
        public string Day;
        public int[] Progress = new int[MissionBook.Count];
        public bool[] Done = new bool[MissionBook.Count];
        public bool Bonus;
    }

    private readonly string path;
    private SaveData data;

    private MissionLog(string path, SaveData data)
    {
        this.path = path;
        this.data = data;
        Normalize();
    }

    public string Day => data.Day;
    public bool Bonus => data.Bonus;

    public static MissionLog Load(string path)
    {
        SaveData loaded = null;
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Missions] Save could not be read, starting fresh: {e.Message}");
        }

        return new MissionLog(path, loaded ?? new SaveData());
    }

    public bool Roll(string day)
    {
        if (data.Day == day) return false;

        data = new SaveData { Day = day };
        return true;
    }

    public int Progress(int index)
    {
        return index >= 0 && index < MissionBook.Count ? data.Progress[index] : 0;
    }

    public bool Done(int index)
    {
        return index >= 0 && index < MissionBook.Count && data.Done[index];
    }

    public int DoneCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < MissionBook.Count; i++)
                if (data.Done[i]) count++;
            return count;
        }
    }

    public bool Raise(int index, int value, int target)
    {
        if (index < 0 || index >= MissionBook.Count || data.Done[index]) return false;

        data.Progress[index] = Mathf.Max(data.Progress[index], Mathf.Min(value, target));
        if (data.Progress[index] < target) return false;

        data.Done[index] = true;
        return true;
    }

    public bool ClaimBonus()
    {
        if (data.Bonus || DoneCount < MissionBook.Count) return false;

        data.Bonus = true;
        return true;
    }

    public bool Save()
    {
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Missions] Save failed: {e.Message}");
            return false;
        }
    }

    private void Normalize()
    {
        if (data.Progress == null || data.Progress.Length != MissionBook.Count) data.Progress = new int[MissionBook.Count];
        if (data.Done == null || data.Done.Length != MissionBook.Count) data.Done = new bool[MissionBook.Count];
    }
}
