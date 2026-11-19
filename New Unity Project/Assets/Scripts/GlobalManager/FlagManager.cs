using UnityEngine;
using System;
using System.Collections.Generic;

public class FlagManager : MonoBehaviour
{
    private static FlagManager instance;
    public static FlagManager Instance => instance;

    private Dictionary<string, bool> flags =
        new Dictionary<string, bool>();

    [Serializable]
    public class FlagEntry
    {
        public string flagName;
        public bool flagValue;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetFlag(string flagName, bool value)
    {
        if (string.IsNullOrWhiteSpace(flagName))
            return;

        flags[flagName] = value;

        // 修改标记后，同步到当前存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveStoryFlags(GetAllFlags());
        }
    }

    public bool GetFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName))
            return false;

        return flags.TryGetValue(flagName, out bool value)
            && value;
    }

    public bool HasFlag(string flagName)
    {
        return !string.IsNullOrWhiteSpace(flagName) &&
            flags.ContainsKey(flagName);
    }

    // 导出所有标记
    public List<FlagEntry> GetAllFlags()
    {
        List<FlagEntry> result = new List<FlagEntry>();

        foreach (var pair in flags)
        {
            result.Add(new FlagEntry
            {
                flagName = pair.Key,
                flagValue = pair.Value
            });
        }

        return result;
    }

    // 从存档恢复标记，不触发自动保存
    public void RestoreFlags(List<FlagEntry> savedFlags)
    {
        flags.Clear();

        if (savedFlags == null)
            return;

        foreach (FlagEntry entry in savedFlags)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.flagName))
                continue;

            flags[entry.flagName] = entry.flagValue;
        }

        Debug.Log($"[FlagManager] 已恢复 {flags.Count} 个剧情标记。");
    }

    // 新游戏使用：清空所有标记
    public void ResetFlags()
    {
        flags.Clear();
    }
}
