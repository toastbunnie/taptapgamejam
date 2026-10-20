
using System.Collections.Generic;
using UnityEngine;
using Game.Items.DragDrop;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    // 记录已经解锁的线索 ID
    private HashSet<string> unlockedClues = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning(
                "[JournalManager] 找不到 SaveManager，无法恢复手记存档。");
            return;
        }

        unlockedClues.Clear();

        List<string> savedClues =
            SaveManager.Instance.LoadUnlockedClues();

        foreach (string clueId in savedClues)
        {
            if (!string.IsNullOrWhiteSpace(clueId))
                unlockedClues.Add(clueId);
        }

        Debug.Log(
            "[JournalManager] 已恢复手记线索，数量：" +
            unlockedClues.Count);
    }

    public void UnlockClue(string clueId, bool showAcquisitionFeedback = true)
    {
        if (string.IsNullOrWhiteSpace(clueId))
        {
            Debug.LogWarning("线索 ID 为空，无法解锁。");
            return;
        }

        if (unlockedClues.Add(clueId))
        {
            Debug.Log("手记解锁新线索：" + clueId);
            if (showAcquisitionFeedback)
            {
                DragDropRuleManager.ShowAcquisitionFeedback(
                    "新线索已记入手记");
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveUnlockedClues(
                    new List<string>(unlockedClues));
            }
            else
            {
                Debug.LogWarning(
                    "[JournalManager] 找不到 SaveManager，线索仅保存在内存中。");
            }
        }
    }

    // 查询线索是否已解锁
    public bool IsClueUnlocked(string clueId)
    {
        bool result = unlockedClues.Contains(clueId);
        return result;
    }

    // 开始新游戏时，清空当前周目的手记线索
    public void ResetClues()
    {
        unlockedClues.Clear();
        Debug.Log("[JournalManager] 已清空当前周目的手记线索。");
    }
}
