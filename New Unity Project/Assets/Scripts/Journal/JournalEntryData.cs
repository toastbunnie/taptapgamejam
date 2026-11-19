
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewJournalEntry",
    menuName = "Game/Journal Entry"
)]
public class JournalEntryData : ScriptableObject
{
    [Header("线索基本信息")]
    public string clueId;
    public string clueTitle;

    [TextArea(3, 8)]
    public string clueContent;

    [Header("线索图片")]
    public Sprite clueImage;

    [Header("所在页组")]
    [Min(0)]
    public int pageGroup = 0;

    [Tooltip("填写剧情标记名后，只有标记为 true 时才显示这条线索。")]
    public string requiredFlagName;
}
