using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Dialogue;
using Game.Core.SceneTransition;

/// <summary>
/// Connects the final dialogue flags to the ending presentation.
/// Place this on an always-active object in the ending scene.
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
    [Header("结局演出")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private EndingEffect endingEffect;
    [SerializeField] private EndingData[] endings;

    private bool endingPlayed;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (endingEffect == null)
            endingEffect = FindObjectOfType<EndingEffect>(true);
    }

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueFinished += HandleDialogueFinished;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueFinished -= HandleDialogueFinished;
    }

    private void HandleDialogueFinished()
    {
        if (endingPlayed)
            return;

        if (FlagManager.Instance == null)
        {
            Debug.LogError("[EndingSequenceController] 找不到 FlagManager。", this);
            return;
        }

        if (endingEffect == null)
        {
            Debug.LogError("[EndingSequenceController] 找不到 EndingEffect。", this);
            return;
        }

        if (endings == null)
            return;

        foreach (EndingData ending in endings)
        {
            if (ending == null || string.IsNullOrWhiteSpace(ending.endingFlag))
                continue;

            if (!FlagManager.Instance.GetFlag(ending.endingFlag))
                continue;

            EndingUnlockStore.Unlock(ending.endingFlag);
            endingPlayed = true;
            endingEffect.PlayEnding(ending);
            return;
        }

        Debug.LogWarning(
            "[EndingSequenceController] 对话已结束，但没有已解锁的结局标记。请检查 DialogueManager 的 dialogueFlags 和 END 台词 ID。",
            this
        );
    }

    public void ReturnToMainMenu()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.FadeToScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }
}
