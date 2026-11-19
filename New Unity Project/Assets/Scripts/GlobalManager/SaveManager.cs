using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Serializable]
    public class DialogueHistoryEntry
    {
        public string sceneName;
        public string speaker;
        public string content;
    }

    [Serializable]
    public class EmotionSegmentSaveData
    {
        public float value;
        public Color color;
    }

    [Serializable]
    public class SaveData
    {
        public string sceneName;
        public int dialogueId;
        public bool hasDialogueProgress;

        public string bgmId = "";
        public bool bgmStopped = false;

        // 当前周目已经解锁的手记线索
        public List<string> unlockedClueIds = new List<string>();

        // 当前存档中的剧情标记
        public List<FlagManager.FlagEntry> storyFlags =
            new List<FlagManager.FlagEntry>();

        // 当前背包物品。旧版存档没有此字段时按空背包处理。
        public List<string> ownedItemIds = new List<string>();

        // 已显示的对话与调查文字，供历史回顾页面使用。
        public List<DialogueHistoryEntry> dialogueHistory =
            new List<DialogueHistoryEntry>();

        // 情绪条状态随现有存档保存，不另建存档文件。
        public bool hasEmotionState;
        public float emotionValue;
        public bool emotionWarmTriggered;
        public bool emotionHotTriggered;
        public Color emotionFillColor = Color.white;
        public List<EmotionSegmentSaveData> emotionSegments =
            new List<EmotionSegmentSaveData>();
    }

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "game_save.json");

    public SaveData CurrentSave { get; private set; }

    public bool HasSave =>
        CurrentSave != null &&
        !string.IsNullOrEmpty(CurrentSave.sceneName);

    private void Start()
    {
        // 等各组件完成 Awake 后再恢复标记
        RestoreStoryFlags();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromDisk();
    }

    public void SaveDialogueProgress(string sceneName, int dialogueId)
    {
        if (CurrentSave == null)
            CurrentSave = new SaveData();

        CurrentSave.sceneName = sceneName;
        CurrentSave.dialogueId = dialogueId;
        CurrentSave.hasDialogueProgress = true;

        WriteToDisk();
    }

    public void AddDialogueHistory(string sceneName, string speaker, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        if (CurrentSave == null)
            CurrentSave = new SaveData();
        if (CurrentSave.dialogueHistory == null)
            CurrentSave.dialogueHistory = new List<DialogueHistoryEntry>();

        CurrentSave.dialogueHistory.Add(new DialogueHistoryEntry
        {
            sceneName = string.IsNullOrWhiteSpace(sceneName)
                ? SceneManager.GetActiveScene().name
                : sceneName,
            speaker = speaker ?? "",
            content = content
        });

        const int maxHistoryEntries = 1000;
        if (CurrentSave.dialogueHistory.Count > maxHistoryEntries)
            CurrentSave.dialogueHistory.RemoveRange(
                0, CurrentSave.dialogueHistory.Count - maxHistoryEntries);

        WriteToDisk();
    }

    public List<DialogueHistoryEntry> GetDialogueHistory()
    {
        if (CurrentSave == null || CurrentSave.dialogueHistory == null)
            return new List<DialogueHistoryEntry>();

        return new List<DialogueHistoryEntry>(CurrentSave.dialogueHistory);
    }

    public void SaveCurrentData()
    {
        if (CurrentSave != null)
            WriteToDisk();
    }

    public void SaveEmotionState(
        float value,
        bool warmTriggered,
        bool hotTriggered,
        Color fillColor,
        List<EmotionSegmentSaveData> segments)
    {
        if (CurrentSave == null)
        {
            CurrentSave = new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };
        }

        CurrentSave.hasEmotionState = true;
        CurrentSave.emotionValue = value;
        CurrentSave.emotionWarmTriggered = warmTriggered;
        CurrentSave.emotionHotTriggered = hotTriggered;
        CurrentSave.emotionFillColor = fillColor;
        CurrentSave.emotionSegments = segments != null
            ? new List<EmotionSegmentSaveData>(segments)
            : new List<EmotionSegmentSaveData>();

        WriteToDisk();
    }

    // 保存当前背景音乐状态，不覆盖已有的剧情进度
    public void SaveBGMState(string bgmId, bool stopped)
    {
        if (CurrentSave == null)
        {
            CurrentSave = new SaveData();
            CurrentSave.sceneName =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().name;
        }

        CurrentSave.bgmId = bgmId ?? "";
        CurrentSave.bgmStopped = stopped;

        WriteToDisk();
    }

    // 保存剧情标记
    public void SaveStoryFlags(
        List<FlagManager.FlagEntry> flags)
    {
        if (CurrentSave == null)
            return;

        CurrentSave.storyFlags = flags != null
            ? new List<FlagManager.FlagEntry>(flags)
            : new List<FlagManager.FlagEntry>();

        WriteToDisk();
    }

    // 将存档中的剧情标记恢复到 FlagManager
    public void RestoreStoryFlags()
    {
        if (FlagManager.Instance == null)
        {
            Debug.LogWarning(
                "[SaveManager] 找不到 FlagManager，暂时无法恢复剧情标记。");
            return;
        }

        List<FlagManager.FlagEntry> savedFlags =
            CurrentSave != null
            ? CurrentSave.storyFlags
            : null;

        FlagManager.Instance.RestoreFlags(savedFlags);
    }

    // 保存当前已解锁的手记线索
    public void SaveUnlockedClues(List<string> clueIds)
    {
        if (CurrentSave == null)
            CurrentSave = new SaveData();

        CurrentSave.unlockedClueIds =
            clueIds != null
                ? new List<string>(clueIds)
                : new List<string>();

        WriteToDisk();
    }

    // 保存当前背包，不覆盖已有的场景、对话或剧情进度。
    public void SaveInventory(List<string> itemIds)
    {
        if (CurrentSave == null)
            return;

        CurrentSave.ownedItemIds = itemIds != null
            ? new List<string>(itemIds)
            : new List<string>();

        WriteToDisk();
    }

    // 从存档恢复背包；不存在 Inventory 时由场景初始化流程处理。
    private void RestoreInventory()
    {
        if (Game.Items.Inventory.Instance == null)
        {
            Debug.LogWarning("[SaveManager] 找不到 Inventory，暂时无法恢复背包物品。");
            return;
        }

        Game.Items.Inventory.Instance.RestoreItems(
            CurrentSave != null ? CurrentSave.ownedItemIds : null);
    }

    // 读取当前存档中的手记线索
    public List<string> LoadUnlockedClues()
    {
        if (CurrentSave == null ||
            CurrentSave.unlockedClueIds == null)
        {
            return new List<string>();
        }

        return new List<string>(CurrentSave.unlockedClueIds);
    }

    public bool TryGetDialogueId(string sceneName, out int dialogueId)
    {
        dialogueId = 0;

        if (CurrentSave == null ||
            !CurrentSave.hasDialogueProgress ||
            CurrentSave.sceneName != sceneName)
            return false;

        dialogueId = CurrentSave.dialogueId;
        return true;
    }

    public void ContinueGame()
    {
        LoadFromDisk();

        if (!HasSave)
        {
            Debug.LogWarning("[SaveManager] 当前没有可继续的存档。");
            return;
        }

        // 在加载场景之前恢复剧情标记
        RestoreStoryFlags();
        RestoreInventory();

        SceneManager.LoadScene(CurrentSave.sceneName);
    }

    public void StartNewGame(string firstSceneName)
    {
        // 迁移旧版本存档中的结局标记，并让图库解锁跨周目保留。
        if (CurrentSave != null)
            EndingUnlockStore.PreserveFromFlags(CurrentSave.storyFlags);
        if (FlagManager.Instance != null)
            EndingUnlockStore.PreserveFromFlags(FlagManager.Instance.GetAllFlags());

        DeleteSave();
        if (Game.Items.Inventory.Instance != null)
            Game.Items.Inventory.Instance.Clear();

        Game.Core.Emotion.EmotionBar.ResetForNewGame();

        if (FlagManager.Instance != null)
        {
            FlagManager.Instance.ResetFlags();
        }
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.ResetClues();
        }
        CurrentSave = new SaveData
        {
            sceneName = firstSceneName,
            hasDialogueProgress = false
        };

        WriteToDisk();
        SceneManager.LoadScene(firstSceneName);
    }

    public void DeleteSave()
    {
        CurrentSave = null;

        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(SavePath))
        {
            CurrentSave = null;
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            CurrentSave = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] 读取存档失败：" + e.Message);
            CurrentSave = null;
        }
    }

    private void WriteToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentSave, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] 保存失败：" + e.Message);
        }
    }
}
