using System.Collections;
using UnityEngine;
using Game.Dialogue;
using Game.Items;
using Game.UI;

/// <summary>
/// Handles the dressing table bird click: transition to the revealed image,
/// play its dialogue, then grant the configured inventory item once.
/// </summary>
public sealed class DressingTableBirdSequence : MonoBehaviour
{
    [Header("画面切换")]
    [SerializeField] private ImageStateTransition imageTransition;

    [Header("对话与奖励")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private int dialogueId = -1;
    [SerializeField] private string rewardItemId;
    [SerializeField] private string clueIdToUnlock;
    [SerializeField] private string completionFlagName;

    private bool sequenceStarted;
    private bool awaitingDialogueStart;
    private bool waitingForDialogue;
    private bool rewardGranted;

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueStarted += OnDialogueStarted;
        if (dialogueManager != null)
            dialogueManager.DialogueFinished += OnDialogueFinished;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueStarted -= OnDialogueStarted;
            dialogueManager.DialogueFinished -= OnDialogueFinished;
        }
    }

    /// <summary>Bind this parameterless method to the bird Button's OnClick.</summary>
    public void OnBirdClicked()
    {
        if (sequenceStarted || rewardGranted)
            return;

        if (imageTransition == null || dialogueManager == null ||
            dialogueId < 0 || string.IsNullOrWhiteSpace(rewardItemId))
        {
            Debug.LogError(
                $"[{nameof(DressingTableBirdSequence)}] 请绑定图片切换、对话管理器、有效剧情 ID 和奖励道具 ID。",
                this);
            return;
        }

        sequenceStarted = true;
        imageTransition.BeginTransition();
        StartCoroutine(StartDialogueAfterTransition());
    }

    private IEnumerator StartDialogueAfterTransition()
    {
        yield return new WaitForSeconds(imageTransition.Duration);
        imageTransition.CompleteTransition();

        awaitingDialogueStart = true;
        dialogueManager.StartMainDialogue(dialogueId);
    }

    private void OnDialogueStarted(int startedDialogueId)
    {
        if (!awaitingDialogueStart)
            return;

        awaitingDialogueStart = false;
        waitingForDialogue = startedDialogueId == dialogueId;
    }

    private void OnDialogueFinished()
    {
        if (!waitingForDialogue)
            return;

        waitingForDialogue = false;
        if (!string.IsNullOrWhiteSpace(completionFlagName))
        {
            if (FlagManager.Instance != null)
                FlagManager.Instance.SetFlag(completionFlagName.Trim(), true);
            else
                Debug.LogWarning(
                    $"[{nameof(DressingTableBirdSequence)}] 找不到 FlagManager，无法设置剧情标记 {completionFlagName}。",
                    this);
        }

        UnlockJournalClue();

        if (Inventory.Instance == null)
        {
            Debug.LogError(
                $"[{nameof(DressingTableBirdSequence)}] 找不到 Inventory，无法发放奖励 {rewardItemId}。",
                this);
            return;
        }

        rewardGranted = Inventory.Instance.Add(rewardItemId);
        if (rewardGranted)
            Debug.Log($"[{nameof(DressingTableBirdSequence)}] 对话结束，获得道具 {rewardItemId}。", this);
    }

    private void UnlockJournalClue()
    {
        if (!string.IsNullOrWhiteSpace(clueIdToUnlock))
        {
            if (JournalManager.Instance == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DressingTableBirdSequence)}] 找不到 JournalManager，无法解锁线索 {clueIdToUnlock}。",
                    this);
            }
            else
            {
                JournalManager.Instance.UnlockClue(clueIdToUnlock.Trim());
            }
        }
    }
}
