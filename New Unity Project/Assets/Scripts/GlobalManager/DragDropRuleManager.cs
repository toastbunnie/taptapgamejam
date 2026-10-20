using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Game.Dialogue;
using Game.UI;

namespace Game.Items.DragDrop
{
    public class DragDropRuleManager : MonoBehaviour
    {
        private static DragDropRuleManager activeInstance;

        [Header("规则反馈 UI")]
        [SerializeField] private TMP_Text feedbackText;
        [Tooltip("拖拽结果文字显示的秒数；设为 0 则不自动清除")]
        [SerializeField, Min(0f)] private float feedbackDisplayDuration = 2f;

        private Coroutine feedbackClearCoroutine;

        [Serializable]
        public class CountRequirement
        {
            [Header("拖拽条件")]
            public string sourceItemId;
            public string targetId;

            [Min(1)]
            public int requiredCount = 1;

            [NonSerialized]
            public int currentCount;
        }

        [Serializable]
        public class DragRule
        {
            [Header("规则名称")]
            public string ruleName;

            [Header("拖拽总次数")]
            [Min(1)]
            public int totalRequiredCount;

            [Header("成功判定条件")]
            public List<CountRequirement> requirements =
                new List<CountRequirement>();

            [Header("拖拽完成切入剧情")]
            public bool playDialogue;
            public int dialogueId;

            [Header("指定台词切换图片")]
            public bool changeImage;
            public int triggerDialogueId;

            [Header("拖拽完成获得道具")]
            public bool grantItem;
            public string rewardItemId;

            [Min(0.01f)]
            public float imageTransitionDuration = 0.8f;

            [NonSerialized]
            public int totalCurrentCount;

            [NonSerialized]
            public bool completed;

            [NonSerialized]
            public bool failed;

            [NonSerialized]
            public bool rewardIssued;
        }

        [Header("可选：场景中的对话管理器")]
        [SerializeField] private DialogueManager dialogueManager;

        [Header("可选：图片切换组件")]
        [SerializeField] private ImageStateTransition imageTransition;

        [Header("拖拽规则列表")]
        [SerializeField] private List<DragRule> rules =
            new List<DragRule>();

        private DragRule activeRule;

        private void OnEnable()
        {
            activeInstance = this;
            SubscribeDialogueEvents();
        }

        private void OnDisable()
        {
            UnsubscribeDialogueEvents();
            if (activeInstance == this)
                activeInstance = null;
        }

        /// <summary>Routes item and journal notifications through this scene's configured FeedbackText.</summary>
        public static void ShowAcquisitionFeedback(string message)
        {
            DragDropRuleManager manager = activeInstance;

            if (manager == null)
            {
                Debug.LogWarning("[DragDropRuleManager] 当前场景未启用 DragDropRuleManager，无法显示获得提示。");
                return;
            }

            manager.ShowFeedback(message);
        }

        public void SetFeedbackText(TMP_Text text)
        {
            feedbackText = text;
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText == null)
                return;

            if (feedbackClearCoroutine != null)
            {
                StopCoroutine(feedbackClearCoroutine);
                feedbackClearCoroutine = null;
            }

            feedbackText.text = message;
            feedbackText.color = new Color32(49, 52, 74, 255);

            if (feedbackDisplayDuration > 0f && !string.IsNullOrEmpty(message))
                feedbackClearCoroutine = StartCoroutine(ClearFeedbackAfterDelay());
        }

        private System.Collections.IEnumerator ClearFeedbackAfterDelay()
        {
            yield return new WaitForSecondsRealtime(feedbackDisplayDuration);

            if (feedbackText != null)
                feedbackText.text = "";

            feedbackClearCoroutine = null;
        }

        private void ShowRuleResult(bool success, string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return;

            if (success)
                ShowFeedback($"{ruleName} 完成！");
            else
                ShowFeedback($"{ruleName} 失败，重新尝试");
        }

        private void SubscribeDialogueEvents()
        {
            if (dialogueManager == null)
                return;

            dialogueManager.DialogueStarted += OnDialogueStarted;
            dialogueManager.DialogueRecordStarted += OnDialogueRecordStarted;
            dialogueManager.DialogueFinished += OnDialogueFinished;
        }

        private void UnsubscribeDialogueEvents()
        {
            if (dialogueManager == null)
                return;

            dialogueManager.DialogueStarted -= OnDialogueStarted;
            dialogueManager.DialogueRecordStarted -= OnDialogueRecordStarted;
            dialogueManager.DialogueFinished -= OnDialogueFinished;
        }

        public void RegisterSuccessfulDrop(
            string sourceItemId,
            string targetId,
            bool fromInventory)
        {
            if (string.IsNullOrWhiteSpace(sourceItemId) ||
                string.IsNullOrWhiteSpace(targetId))
            {
                Debug.LogWarning(
                    "[DragDropRuleManager] 来源 ID 或目标 ID 为空。");
                return;
            }

            foreach (DragRule rule in rules)
            {
                if (rule == null || rule.completed)
                    continue;

                if (rule.requirements == null ||
                    rule.requirements.Count == 0)
                    continue;

                // 一条规则只统计它配置的接收目标。
                bool belongsToTarget = false;

                foreach (CountRequirement requirement in rule.requirements)
                {
                    if (requirement != null &&
                        requirement.targetId == targetId)
                    {
                        belongsToTarget = true;
                        break;
                    }
                }

                // 其他目标的拖拽不影响这条规则。
                if (!belongsToTarget)
                    continue;

                // 失败状态只用于记录日志，计数已经重置，
                // 下一次收到拖拽时直接开始新一轮。
                rule.failed = false;

                // 每次该目标收到拖拽，都计入总次数。
                rule.totalCurrentCount++;

                // 只有物品 ID 和目标 ID 同时匹配才累计具体条件。
                foreach (CountRequirement requirement in rule.requirements)
                {
                    if (requirement == null)
                        continue;

                    if (requirement.sourceItemId == sourceItemId &&
                        requirement.targetId == targetId)
                    {
                        requirement.currentCount++;

                        // 同一个目标、同一种物品只累计一次。
                        break;
                    }
                }

                Debug.Log(
                    $"[拖拽规则] {rule.ruleName}，目标 {targetId}：" +
                    $"{rule.totalCurrentCount}/{rule.totalRequiredCount}");

                // 还没达到本轮规定次数，继续等待。
                if (rule.totalCurrentCount < rule.totalRequiredCount)
                    continue;

                // 超过规定次数，判定本轮失败。
                if (rule.totalCurrentCount > rule.totalRequiredCount)
                {
                    Debug.LogWarning(
                        $"[拖拽规则] {rule.ruleName} 失败：总次数超出要求。");

                    ShowRuleResult(false, rule.ruleName);
                    ResetRuleCounts(rule);
                    rule.failed = true;
                    continue;
                }

                // 总次数恰好达到要求，严格检查所有具体条件。
                if (IsRuleSatisfied(rule))
                {
                    rule.completed = true;

                    Debug.Log(
                        $"[拖拽规则] {rule.ruleName}：所有条件满足。");

                    ShowRuleResult(true, rule.ruleName);
                    ExecuteRule(rule);
                }
                else
                {
                    Debug.LogWarning(
                        $"[拖拽规则] {rule.ruleName} 失败：" +
                        "具体拖拽次数不符合要求。本轮计数已重置。");

                    ShowRuleResult(false, rule.ruleName);
                    ResetRuleCounts(rule);
                    rule.failed = true;
                }
            }
        }

        private bool IsRuleSatisfied(DragRule rule)
        {
            if (rule == null ||
                rule.requirements == null ||
                rule.requirements.Count == 0)
                return false;

            foreach (CountRequirement requirement in rule.requirements)
            {
                if (requirement == null)
                    return false;

                if (requirement.currentCount != requirement.requiredCount)
                    return false;
            }

            return true;
        }

        private void ExecuteRule(DragRule rule)
        {
            Debug.Log($"[拖拽规则] 条件满足：{rule.ruleName}");

            activeRule = rule;

            if (rule.changeImage && imageTransition != null)
                imageTransition.ResetToState1();

            if (rule.playDialogue)
            {
                if (dialogueManager == null)
                {
                    Debug.LogError(
                        "[拖拽规则] 已启用对话，但没有绑定 DialogueManager。");

                    activeRule = null;
                    return;
                }

                dialogueManager.StartMainDialogue(rule.dialogueId);
                if (!dialogueManager.IsDialogueActive)
                {
                    // 无法启动配置的对白时，避免规则一直占用并漏发奖励。
                    IssueReward(rule);
                    activeRule = null;
                }
                return;
            }

            // 不带对白的规则在判定成功后立即发奖。
            IssueReward(rule);

            // 没有对话时，图片变化直接发生。
            if (rule.changeImage)
            {
                if (imageTransition != null)
                    imageTransition.BeginTransition(
                        rule.imageTransitionDuration);
                else
                    Debug.LogWarning(
                        "[拖拽规则] 已启用图片变化，但没有绑定图片组件。");
            }

            activeRule = null;
        }

        private void OnDialogueStarted(int dialogueId)
        {
            // 当前规则已经决定了图片的初始状态。
        }

        private void OnDialogueRecordStarted(int dialogueId)
        {
            if (activeRule == null || !activeRule.changeImage)
                return;

            if (dialogueId != activeRule.triggerDialogueId)
                return;

            if (imageTransition == null)
            {
                Debug.LogWarning(
                    "[拖拽规则] 没有绑定 ImageStateTransition。");
                return;
            }

            imageTransition.BeginTransition(
                activeRule.imageTransitionDuration);

            Debug.Log(
                $"[拖拽规则] 在台词 ID {dialogueId} 触发图片变化。");
        }

        private void OnDialogueFinished()
        {
            if (activeRule == null)
                return;

            if (activeRule.changeImage && imageTransition != null)
                imageTransition.CompleteTransition();

            IssueReward(activeRule);

            activeRule = null;
        }

        private void IssueReward(DragRule rule)
        {
            if (rule == null || !rule.grantItem || rule.rewardIssued)
                return;

            if (Inventory.Instance == null)
            {
                Debug.LogError(
                    $"[拖拽规则] {rule.ruleName}：找不到 Inventory，无法发放奖励。");
                return;
            }

            if (string.IsNullOrWhiteSpace(rule.rewardItemId))
            {
                Debug.LogWarning(
                    $"[拖拽规则] {rule.ruleName}：奖励道具 ID 为空。");
                return;
            }

            Inventory.Instance.Add(rule.rewardItemId);
            rule.rewardIssued = true;

            Debug.Log(
                $"[拖拽规则] {rule.ruleName}：获得道具 {rule.rewardItemId}");
        }

        private void ResetRuleCounts(DragRule rule)
        {
            if (rule == null)
                return;

            rule.totalCurrentCount = 0;

            if (rule.requirements != null)
            {
                foreach (CountRequirement requirement in rule.requirements)
                {
                    if (requirement != null)
                        requirement.currentCount = 0;
                }
            }

            rule.failed = false;
        }
    }
}
