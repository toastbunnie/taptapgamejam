using System.Collections.Generic;
using UnityEngine;

/// <summary>Stores ending gallery unlocks independently from per-run story flags.</summary>
public static class EndingUnlockStore
{
    private const string KeyPrefix = "EndingGalleryUnlocked_";

    public static bool IsUnlocked(string endingFlag)
    {
        if (string.IsNullOrWhiteSpace(endingFlag))
            return false;

        return PlayerPrefs.GetInt(KeyPrefix + endingFlag.Trim(), 0) == 1;
    }

    public static void Unlock(string endingFlag)
    {
        if (string.IsNullOrWhiteSpace(endingFlag))
            return;

        PlayerPrefs.SetInt(KeyPrefix + endingFlag.Trim(), 1);
        PlayerPrefs.Save();
    }

    public static void PreserveFromFlags(IEnumerable<FlagManager.FlagEntry> flags)
    {
        if (flags == null)
            return;

        bool changed = false;
        foreach (FlagManager.FlagEntry entry in flags)
        {
            if (entry == null || !entry.flagValue ||
                string.IsNullOrWhiteSpace(entry.flagName) ||
                !entry.flagName.StartsWith("ending", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string key = KeyPrefix + entry.flagName.Trim();
            if (PlayerPrefs.GetInt(key, 0) == 1)
                continue;

            PlayerPrefs.SetInt(key, 1);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();
    }
}
