using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [Header("手记界面")]
    public GameObject journalPanel;
    public GameObject darkOverlay;

    public bool IsOpen => journalPanel != null && journalPanel.activeInHierarchy;

    [Header("线索卡片")]
    public JournalClueView[] clueViews;

    [Header("翻页按钮")]
    public GameObject previousButton;
    public GameObject nextButton;

    [Header("页数设置")]
    [Min(1)]
    public int totalPageGroups = 3;

    [Header("组合结果提示")]
    public TMP_Text combinationResultText;

    [Serializable]
    public class ClueCombinationRule
    {
        [Tooltip("输入线索 A 的 clueId")]
        public string clueA;

        [Tooltip("输入线索 B 的 clueId")]
        public string clueB;

        [Tooltip("组合后解锁线索 C 的 clueId")]
        public string resultClue;
    }

    [Header("线索组合规则（A + B = C）")]
    [Tooltip("可添加多条规则；A、B 的选择顺序不限。")]
    [SerializeField] private List<ClueCombinationRule> combinationRules =
        new List<ClueCombinationRule>();

    private JournalClueView firstSelectedClue;
    private JournalClueView secondSelectedClue;
    private bool isReasoningMode = false;

    // 当前页组，从 0 开始
    private int currentPageGroup = 0;

    public void OpenJournal()
    {
        currentPageGroup = 0;

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
            Image overlayImage = darkOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.raycastTarget = true;
        }

        if (journalPanel != null)
        {
            journalPanel.SetActive(true);

            CanvasGroup panelGroup = journalPanel.GetComponent<CanvasGroup>();
            if (panelGroup != null)
                panelGroup.blocksRaycasts = true;

            Image panelImage = journalPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = true;
        }

        ShowMessage("");
        UpdatePageDisplay();
    }

    public void CloseJournal()
    {
        if (journalPanel != null)
        {
            journalPanel.SetActive(false);

            CanvasGroup panelGroup = journalPanel.GetComponent<CanvasGroup>();
            if (panelGroup != null)
                panelGroup.blocksRaycasts = false;

            Image panelImage = journalPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = false;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
            Image overlayImage = darkOverlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.raycastTarget = false;
        }
    }

    public void NextPage()
    {
        if (currentPageGroup < totalPageGroups - 1)
        {
            currentPageGroup++;
            UpdatePageDisplay();
        }
    }

    public void PreviousPage()
    {
        if (currentPageGroup > 0)
        {
            currentPageGroup--;
            UpdatePageDisplay();
        }
    }

    private void UpdatePageDisplay()
    {
        if (previousButton != null)
            previousButton.SetActive(currentPageGroup > 0);

        if (nextButton != null)
            nextButton.SetActive(
                currentPageGroup < totalPageGroups - 1
            );

        if (clueViews != null)
        {
            foreach (JournalClueView clueView in clueViews)
            {
                if (clueView != null)
                    clueView.Refresh(currentPageGroup);
            }
        }
    }

    // 统一使用 CombinationResultText 显示所有提示
    private void ShowMessage(string message)
    {
        if (combinationResultText != null)
            combinationResultText.text = message;
    }

    public void ToggleClueSelection(JournalClueView clue)
    {
        if (clue == null)
            return;

        if (!isReasoningMode)
        {
            ShowMessage("请先打开推理模式。");
            return;
        }

        if (firstSelectedClue == clue)
        {
            firstSelectedClue.SetSelected(false);
            firstSelectedClue = secondSelectedClue;
            secondSelectedClue = null;
        }
        else if (secondSelectedClue == clue)
        {
            secondSelectedClue.SetSelected(false);
            secondSelectedClue = null;
        }
        else if (firstSelectedClue == null)
        {
            firstSelectedClue = clue;
            clue.SetSelected(true);
        }
        else if (secondSelectedClue == null)
        {
            secondSelectedClue = clue;
            clue.SetSelected(true);
        }
        else
        {
            ShowMessage("最多选择两条线索。");
            return;
        }

        if (firstSelectedClue != null &&
            secondSelectedClue != null)
        {
            ShowMessage("已选择两条线索，点击推理进行组合。");
        }
        else if (firstSelectedClue != null)
        {
            ShowMessage("已选择一条线索，请再选择一条。");
        }
        else
        {
            ShowMessage("请选择两条线索进行组合。");
        }
    }

    public void ToggleReasoningPanel()
    {
        isReasoningMode = !isReasoningMode;

        if (!isReasoningMode)
        {
            ClearSelectedClues();
            ShowMessage("已退出推理模式。");
        }
        else
        {
            ShowMessage("已打开推理模式，请选择两条线索。");
        }

        Debug.Log(isReasoningMode
            ? "已进入推理模式"
            : "已退出推理模式");
    }

    public void CombineSelectedClues()
    {
        if (!isReasoningMode)
        {
            ShowMessage("请先打开推理模式。");
            return;
        }

        if (firstSelectedClue == null ||
            secondSelectedClue == null)
        {
            ShowMessage("请先选择两条线索。");
            return;
        }

        if (firstSelectedClue.entryData == null ||
            secondSelectedClue.entryData == null)
        {
            ShowMessage("线索数据缺失，无法进行推理。");
            ClearSelectedClues(false);
            return;
        }

        string clueA = firstSelectedClue.entryData.clueId;
        string clueB = secondSelectedClue.entryData.clueId;

        ClueCombinationRule matchedRule = FindCombinationRule(clueA, clueB);

        if (matchedRule != null)
        {
            if (JournalManager.Instance == null)
            {
                ShowMessage("找不到手记管理器，无法解锁线索。");
                ClearSelectedClues(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(matchedRule.resultClue))
            {
                ShowMessage("组合规则没有填写结果线索 ID。");
                Debug.LogWarning("[手记] 匹配到组合规则，但 resultClue 为空。", this);
                ClearSelectedClues(false);
                return;
            }

            bool wasAlreadyUnlocked =
                JournalManager.Instance.IsClueUnlocked(matchedRule.resultClue);
            // 推理结果已经在手记面板内反馈，不再弹出获得提示。
            JournalManager.Instance.UnlockClue(
                matchedRule.resultClue,
                showAcquisitionFeedback: false);

            ShowMessage(wasAlreadyUnlocked
                ? "这条线索已经推理出来了。"
                : "发现了新的线索！");

            Debug.Log($"[手记] 推理成功：{clueA} + {clueB} = {matchedRule.resultClue}。");
            RefreshClueViews();
        }
        else
        {
            ShowMessage("这两条线索似乎没有直接联系。");
            Debug.Log("推理失败：这两条线索无法组合。");
        }

        // 清除选中状态，但保留刚刚显示的结果文字。
        ClearSelectedClues(false);
    }

    private ClueCombinationRule FindCombinationRule(string clueA, string clueB)
    {
        if (combinationRules == null)
            return null;

        foreach (ClueCombinationRule rule in combinationRules)
        {
            if (rule == null ||
                string.IsNullOrWhiteSpace(rule.clueA) ||
                string.IsNullOrWhiteSpace(rule.clueB))
                continue;

            bool matches =
                (string.Equals(rule.clueA, clueA, StringComparison.Ordinal) &&
                 string.Equals(rule.clueB, clueB, StringComparison.Ordinal)) ||
                (string.Equals(rule.clueA, clueB, StringComparison.Ordinal) &&
                 string.Equals(rule.clueB, clueA, StringComparison.Ordinal));

            if (matches)
                return rule;
        }

        return null;
    }

    private void RefreshClueViews()
    {
        if (clueViews == null)
            return;

        foreach (JournalClueView view in clueViews)
        {
            if (view != null)
                view.Refresh(currentPageGroup);
        }
    }

    private void ClearSelectedClues(bool clearMessage = true)
    {
        if (firstSelectedClue != null)
            firstSelectedClue.SetSelected(false);

        if (secondSelectedClue != null)
            secondSelectedClue.SetSelected(false);

        firstSelectedClue = null;
        secondSelectedClue = null;

        if (clearMessage)
            ShowMessage("");
    }
}
