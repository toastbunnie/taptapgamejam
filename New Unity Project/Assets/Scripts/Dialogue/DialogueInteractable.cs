using UnityEngine;
using Game.Core.Interaction;
using Game.Dialogue;

// 可复用的点击交互组件：显示调查文字，或直接开始指定主线剧情。
// 需要与 SpriteRenderer + BoxCollider2D 放在同一个物体上。
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    public enum InteractionType
    {
        TemporaryInfo,   // 显示调查文字；结束后返回探索
        InfoThenStory,   // 显示调查文字；结束后开始指定主线
        StartStory       // 点击后直接开始指定主线
    }

    [Header("交互设置")]
    [SerializeField] private InteractionType interactionType = InteractionType.TemporaryInfo;

    [Header("剧情解锁条件")]
    [SerializeField] private bool requireFlag = false;
    [SerializeField] private string requiredFlagName = "";
    [SerializeField] private bool requiredFlagValue = true;

    [Header("已调查/已触发标记")]
    [SerializeField] private string inspectionFlagName = "";
    [SerializeField] private bool inspectionFlagValue = true;

    [Header("重复点击行为")]
    [Tooltip("启用后首次点击执行原交互，之后显示已调查信息。")]
    [SerializeField] private bool useRepeatInteractionInfo = true;

    [Header("点击时解锁手记线索")]
    [Tooltip("填写 JournalEntryData.clueId；留空则不解锁线索")]
    [SerializeField] private string clueIdToUnlock = "";

    [Header("点击显示的信息")]
    [TextArea(2, 5)]
    [SerializeField] private string infoText;

    [Header("已调查后的重复点击信息")]
    [TextArea(2, 5)]
    [SerializeField] private string alreadyInspectedInfoText;

    private const string DefaultAlreadyInspectedInfoText = "你已经调查过这里了。";
    
    private string displayName;
    private string position;
    private Sprite avatar;

    [Header("点击切入的剧情ID（InfoThenStory、StartStory模式必填）")]
    [SerializeField] private int storyDialogueId = -1;

    public string PromptText => "点击";

    public bool CanInteract
    {
        get
        {
            if (!requireFlag)
                return true;

            if (FlagManager.Instance == null)
                return false;

            return FlagManager.Instance.GetFlag(requiredFlagName)
                == requiredFlagValue;
        }
    }

    public void Interact(GameObject player)
    {
        DialogueManager manager = FindObjectOfType<DialogueManager>();
        if (manager == null)
        {
            Debug.LogError($"[{nameof(DialogueInteractable)}] 场景中没有找到 DialogueManager。", this);
            return;
        }

        if ((interactionType == InteractionType.InfoThenStory ||
             interactionType == InteractionType.StartStory) && storyDialogueId < 0)
        {
            Debug.LogError($"[{nameof(DialogueInteractable)}] {name} 需要填写有效的主线剧情 ID。", this);
            return;
        }

        bool shouldTrackInspection =
            useRepeatInteractionInfo ||
            !string.IsNullOrWhiteSpace(inspectionFlagName);
        bool wasAlreadyInspected = false;

        if (shouldTrackInspection)
        {
            if (FlagManager.Instance == null)
            {
                if (!string.IsNullOrWhiteSpace(inspectionFlagName))
                {
                    Debug.LogError("场景中没有找到 FlagManager。", this);
                    return;
                }
            }
            else
            {
                string effectiveFlagName = GetEffectiveInspectionFlagName();
                wasAlreadyInspected =
                    FlagManager.Instance.HasFlag(effectiveFlagName) &&
                    FlagManager.Instance.GetFlag(effectiveFlagName)
                    == inspectionFlagValue;

                if (!wasAlreadyInspected)
                {
                    FlagManager.Instance.SetFlag(
                        effectiveFlagName,
                        inspectionFlagValue
                    );
                }
            }

            if (useRepeatInteractionInfo && wasAlreadyInspected)
            {
                manager.ShowTemporaryInfo(
                    string.IsNullOrWhiteSpace(alreadyInspectedInfoText)
                        ? DefaultAlreadyInspectedInfoText
                        : alreadyInspectedInfoText,
                    displayName,
                    position,
                    avatar
                );
                return;
            }
        }

        switch (interactionType)
        {
            case InteractionType.TemporaryInfo:
                manager.ShowTemporaryInfo(infoText, displayName, position, avatar);
                UnlockJournalClue();
                break;

            case InteractionType.InfoThenStory:
                manager.ShowTemporaryInfo(infoText, displayName, position, avatar, storyDialogueId);
                UnlockJournalClue();
                break;

            case InteractionType.StartStory:
                manager.StartMainDialogue(storyDialogueId);
                UnlockJournalClue();
                break;
        }
    }

    private string GetEffectiveInspectionFlagName()
    {
        if (!string.IsNullOrWhiteSpace(inspectionFlagName))
            return inspectionFlagName.Trim();

        string objectPath = $"{transform.name}[{transform.GetSiblingIndex()}]";
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
        {
            objectPath =
                $"{parent.name}[{parent.GetSiblingIndex()}]/{objectPath}";
        }

        return $"interaction:{gameObject.scene.name}:{objectPath}";
    }

    private void UnlockJournalClue()
    {
        if (string.IsNullOrWhiteSpace(clueIdToUnlock))
            return;

        if (JournalManager.Instance == null)
        {
            Debug.LogWarning(
                $"[{nameof(DialogueInteractable)}] 点击 {name} 后找不到 JournalManager，无法解锁线索 {clueIdToUnlock}。",
                this
            );
            return;
        }

        JournalManager.Instance.UnlockClue(clueIdToUnlock.Trim());
    }
}
