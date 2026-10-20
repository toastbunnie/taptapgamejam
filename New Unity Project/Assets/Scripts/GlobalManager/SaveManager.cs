using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

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
