using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalClueView : MonoBehaviour
{
    [Header("线索数据")]
    public JournalEntryData entryData;

    [Header("显示内容")]
    public GameObject visualRoot;
    public TMP_Text titleText;
    public TMP_Text contentText;
    public Image clueImage;

    [Header("推理状态")]
    public GameObject selectedHighlight;

    public bool IsSelected { get; private set; }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    public void Refresh(int currentPageGroup)
    {
        if (entryData == null)
        {
            Debug.LogError(
                "[手记] 没有绑定线索数据：" + gameObject.name
            );

            SetVisible(false);
            return;
        }

        if (JournalManager.Instance == null)
        {
            Debug.LogError("[手记] 找不到 JournalManager！");
            SetVisible(false);
            return;
        }

        bool unlocked =
            JournalManager.Instance.IsClueUnlocked(
                entryData.clueId
            );

        if (unlocked && !string.IsNullOrWhiteSpace(entryData.requiredFlagName))
        {
            unlocked = FlagManager.Instance != null &&
                FlagManager.Instance.GetFlag(entryData.requiredFlagName);
        }

        bool onCurrentPage =
            entryData.pageGroup == currentPageGroup;

        // 未解锁的线索不能保留选中状态。
        if (!unlocked)
            SetSelected(false);

        // 翻页只隐藏不属于当前页的线索，不取消其选中状态。
        SetVisible(unlocked && onCurrentPage);

        if (titleText != null)
            titleText.text = entryData.clueTitle;

        if (contentText != null)
            contentText.text = entryData.clueContent;

        if (clueImage != null)
        {
            clueImage.sprite = entryData.clueImage;
            clueImage.enabled = entryData.clueImage != null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (visualRoot != null)
            visualRoot.SetActive(visible);

        // 当前页不可见时，禁止点击，但不清除选中状态。
        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = visible;

        // 高亮只在卡片可见时显示。
        if (selectedHighlight != null)
            selectedHighlight.SetActive(visible && IsSelected);
    }
}
