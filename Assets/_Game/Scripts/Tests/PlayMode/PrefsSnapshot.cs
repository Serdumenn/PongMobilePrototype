using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed class PrefsSnapshot
{
    private static readonly string[] IntKeys = { "bestScore", "bestScore_rush", "HapticsEnabled", "SoundEnabled", "HintRunsShown", "DailyBest", "CloudBestsReset" };
    private static readonly string[] StringKeys = { "SelectedMode", "LastInterstitialClosedUtc", "DailyBestDay", "OnlineLook", "Language" };

    [Serializable]
    private sealed class Data
    {
        public List<string> IntKeys = new List<string>();
        public List<int> IntValues = new List<int>();
        public List<string> StringKeys = new List<string>();
        public List<string> StringValues = new List<string>();
    }

    private static string BackupPath => Path.Combine(Application.temporaryCachePath, "pingi_test_prefs.json");

    private readonly Data data;

    private PrefsSnapshot(Data data)
    {
        this.data = data;
    }

    public static PrefsSnapshot Capture()
    {
        RecoverInterruptedRun();

        var data = new Data();
        foreach (var key in IntKeys)
        {
            if (!PlayerPrefs.HasKey(key)) continue;
            data.IntKeys.Add(key);
            data.IntValues.Add(PlayerPrefs.GetInt(key));
        }

        foreach (var key in StringKeys)
        {
            if (!PlayerPrefs.HasKey(key)) continue;
            data.StringKeys.Add(key);
            data.StringValues.Add(PlayerPrefs.GetString(key));
        }

        File.WriteAllText(BackupPath, JsonUtility.ToJson(data));
        return new PrefsSnapshot(data);
    }

    public static void UseCleanProfile()
    {
        foreach (var key in IntKeys) PlayerPrefs.DeleteKey(key);
        foreach (var key in StringKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.SetString("SelectedMode", "classic");
        PlayerPrefs.SetInt("HintRunsShown", 3);
        PlayerPrefs.SetInt("HapticsEnabled", 0);
        PlayerPrefs.SetInt("SoundEnabled", 0);
    }

    public void Restore()
    {
        Apply(data);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
    }

    private static void RecoverInterruptedRun()
    {
        if (!File.Exists(BackupPath)) return;

        var stale = JsonUtility.FromJson<Data>(File.ReadAllText(BackupPath));
        if (stale != null) Apply(stale);
        File.Delete(BackupPath);
    }

    private static void Apply(Data data)
    {
        foreach (var key in IntKeys)
        {
            int index = data.IntKeys.IndexOf(key);
            if (index >= 0) PlayerPrefs.SetInt(key, data.IntValues[index]);
            else PlayerPrefs.DeleteKey(key);
        }

        foreach (var key in StringKeys)
        {
            int index = data.StringKeys.IndexOf(key);
            if (index >= 0) PlayerPrefs.SetString(key, data.StringValues[index]);
            else PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }
}
