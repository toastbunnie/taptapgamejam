using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Audio;

namespace Game.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        [System.Serializable]
        public class AvatarEntry
        {
            public string characterName; // 角色名，例如：主角
            public string expression;    // 表情，例如：惊恐；默认表情留空
            public Sprite avatar;        // 对应头像图片
        }

        [Header("剧情文件")]
        [SerializeField] private DialogueDataReader dataReader;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("剧情涉及人物")]
        [SerializeField] private AvatarEntry[] avatarEntries;

        [Header("第一句ID")]
        [SerializeField] private int startId = 0;

        [System.Serializable]
        public class DialogueFlagEntry
        {
            [Tooltip("触发标记的剧情ID（必须是END）")]
            public int endDialogueId;

            [Tooltip("要设置的标记名称")]
            public string flagName;

            [Tooltip("要设置的标记值")]
            public bool flagValue = true;
        }

        [Header("标记列表")]
        [SerializeField] private DialogueFlagEntry[] dialogueFlags;

        [Tooltip("开启：进入场景时自动播放开场/存档剧情；关闭：进入场景后直接探索。")]
        [SerializeField] private bool autoPlayOnSceneEnter = true;

        private int currentId;
        private bool isFinished;
        private bool isTemporaryInfo;
        private int temporaryNextStoryId = -1;
        private AudioDatabase audioDatabase;

        // 一段主线对话开始
        public event Action<int> DialogueStarted;

        // 每条普通台词开始显示，参数是该台词的 ID
        public event Action<int> DialogueRecordStarted;

        // 一段主线对话真正结束
        public event Action DialogueFinished;

        // 当前是否正在显示对话（主线或临时调查信息）。
        public bool IsDialogueVisible => dialogueUI != null && dialogueUI.gameObject.activeInHierarchy;

        // 当前是否正在播放主线剧情或临时调查信息。
        public bool IsDialogueActive => !isFinished && IsDialogueVisible;

        private void Start()
        {
            if (dataReader == null || dialogueUI == null)
            {
                Debug.LogError(
                    "[DialogueManager] 请绑定 DialogueDataReader 和 DialogueUI。");
                enabled = false;
                return;
            }

            if (!dataReader.Load())
            {
                enabled = false;
                return;
            }

            // 仅在允许自动开场时读取并恢复本场景的主线对话进度。
            if (autoPlayOnSceneEnter)
            {
                if (SaveManager.Instance != null &&
                    SaveManager.Instance.TryGetDialogueId(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                        out int savedId))
                {
                    currentId = savedId;
                }
                else
                {
                    currentId = startId;
                }
            }
            else
            {
                currentId = startId;
                isFinished = true;
                dialogueUI.HideDialogue();
            }

            audioDatabase = FindObjectOfType<AudioDatabase>();
            if (audioDatabase == null)
                Debug.LogWarning("[DialogueManager] 场景中没有找到 AudioDatabase，剧情仍可播放，但音频列不会生效。");

            RestoreSavedBGM();

            if (autoPlayOnSceneEnter)
                ShowCurrentDialogue();
        }

        private void Update()
        {
            if (isFinished)
                return;

            bool spacePressed = Input.GetKeyDown(KeyCode.Space);
            bool mousePressed = Input.GetMouseButtonDown(0);

            if (!spacePressed && !mousePressed)
                return;

            // 普通情况下，鼠标点击 UI 不推进对话，避免点选项/按钮时同时推进剧情。
            // 水槽特写打开时，特写遮罩和对话遮挡层覆盖整个屏幕；允许点击这些
            // 纯视觉区域推进对话，但仍拦截按钮与背包槽位上的点击/拖拽。
            if (mousePressed &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                if (!SinkCloseupInteractable.IsAnyOpen || IsPointerOverInteractiveControl())
                    return;
            }

            // 临时调查信息使用同一套打字机和输入规则，但不读写 currentId。
            if (isTemporaryInfo)
            {
                HandleTemporaryInfoInput();
                return;
            }

            if (!dataReader.TryGetRecord(
                    currentId, out DialogueRecord record))
                return;

            // 选项阶段只允许点击选项按钮。
            if (record.flag == "&")
                return;

            HandleAdvanceInput();
        }

        private void HandleTemporaryInfoInput()
        {
            // 打字期间：先显示完整调查文字。
            if (dialogueUI.IsTyping)
            {
                dialogueUI.CompleteTyping();
                return;
            }

            int nextStoryId = temporaryNextStoryId;
            isTemporaryInfo = false;
            temporaryNextStoryId = -1;
            dialogueUI.HideDialogue();

            // 关键物品：调查信息结束后，开始指定的主线剧情。
            if (nextStoryId >= 0)
                StartMainDialogue(nextStoryId);
            else
                isFinished = true;
        }

        private bool IsPointerOverInteractiveControl()
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject.GetComponentInParent<Button>() != null ||
                    hit.gameObject.GetComponentInParent<Game.UI.InventorySlot>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        // 从指定 ID 开始主线剧情。主线进度仍按原有机制保存。
        // 可用于角色点击或调查信息结束后的剧情衔接。
        public void StartMainDialogue(int dialogueId)
        {
            if (dataReader == null || dialogueUI == null)
            {
                Debug.LogError("[DialogueManager] 无法开始剧情：未绑定数据读取器或对话 UI。");
                return;
            }

            if (!dataReader.TryGetRecord(dialogueId, out DialogueRecord record))
            {
                Debug.LogError($"[DialogueManager] 找不到指定剧情 ID：{dialogueId}");
                return;
            }

            UIPanelExclusivity.TryOpen(UIPanelExclusivity.Panel.Dialogue);

            isTemporaryInfo = false;
            temporaryNextStoryId = -1;
            isFinished = false;
            currentId = dialogueId;

            SaveCurrentProgress();

            // 通知订阅者：一段主线对话开始
            DialogueStarted?.Invoke(dialogueId);

            ShowCurrentDialogue();
        }

        // 显示一段临时调查信息。nextStoryId 小于 0 表示结束后返回探索；
        // 否则在玩家读完并再次输入后，开始指定主线剧情。
        // 临时信息不会修改主线 currentId 或存档中的主线对话进度。
        public void ShowTemporaryInfo(
            string content,
            string characterName,
            string position,
            Sprite avatar = null,
            int nextStoryId = -1)
        {
            if (dialogueUI == null)
            {
                Debug.LogError("[DialogueManager] 无法显示调查信息：未绑定 DialogueUI。");
                return;
            }

            UIPanelExclusivity.TryOpen(UIPanelExclusivity.Panel.Dialogue);

            isTemporaryInfo = true;
            temporaryNextStoryId = nextStoryId;
            isFinished = false;

            dialogueUI.ShowDialogue(
                characterName,
                content,
                position,
                avatar
            );
        }

        private void HandleAdvanceInput()
        {
            // 打字期间：先显示完整台词，不推进下一句。
            if (dialogueUI.IsTyping)
            {
                dialogueUI.CompleteTyping();
                return;
            }

            AdvanceDialogue();
        }

        private Sprite GetAvatar(string characterName)
        {
            string roleName = characterName.Trim();
            string expression = "";

            // 识别中文括号或英文括号中的表情。
            int left = roleName.LastIndexOf('（');
            int right = roleName.LastIndexOf('）');

            if (left >= 0 && right == roleName.Length - 1 && right > left)
            {
                expression = roleName.Substring(
                    left + 1, right - left - 1).Trim();

                roleName = roleName.Substring(0, left).Trim();
            }
            else
            {
                left = roleName.LastIndexOf('(');
                right = roleName.LastIndexOf(')');

                if (left >= 0 && right == roleName.Length - 1 && right > left)
                {
                    expression = roleName.Substring(
                        left + 1, right - left - 1).Trim();

                    roleName = roleName.Substring(0, left).Trim();
                }
            }

            // 优先查找角色名和表情都匹配的头像。
            foreach (AvatarEntry entry in avatarEntries)
            {
                if (entry.characterName == roleName &&
                    entry.expression == expression)
                {
                    return entry.avatar;
                }
            }

            // 找不到指定表情时，退回该角色的默认头像。
            foreach (AvatarEntry entry in avatarEntries)
            {
                if (entry.characterName == roleName &&
                    string.IsNullOrWhiteSpace(entry.expression))
                {
                    Debug.LogWarning(
                        $"没有找到 {roleName} 的“{expression}”表情，使用默认头像。");

                    return entry.avatar;
                }
            }

            Debug.LogWarning($"没有找到人物头像配置：{characterName}");
            return null;
        }

        private void ShowCurrentDialogue()
        {
            dialogueUI.ClearOptions();

            if (!dataReader.TryGetRecord(
                    currentId, out DialogueRecord record))
            {
                Debug.LogError(
                    $"[DialogueManager] 找不到剧情 ID：{currentId}");
                FinishDialogue();
                return;
            }

            if (record.flag == "END")
            {
                ApplyDialogueFlags(currentId);
                FinishDialogue();
                return;
            }

            if (record.flag == "&")
            {
                ShowOptionGroup();
                return;
            }

            // 每条普通台词只在显示时触发一次音频；空白字段不改变当前音频。
            HandleAudio(record);

            string displayName = record.character.Trim();

            int bracketIndex = displayName.LastIndexOf('（');
            if (bracketIndex < 0)
                bracketIndex = displayName.LastIndexOf('(');

            if (bracketIndex >= 0)
                displayName = displayName.Substring(0, bracketIndex).Trim();

            // 通知订阅者：当前 ID 对应的台词即将显示
            DialogueRecordStarted?.Invoke(currentId);

            dialogueUI.ShowDialogue(
                displayName,
                record.content,
                record.position,
                GetAvatar(record.character)
            );
        }

        private void HandleAudio(DialogueRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.bgm) &&
                string.IsNullOrWhiteSpace(record.sfx))
                return;

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[DialogueManager] 找不到 AudioManager，无法播放音频。");
                return;
            }

            if (!string.IsNullOrWhiteSpace(record.bgm))
            {
                string bgmId = record.bgm.Trim();

                if (string.Equals(
                    bgmId, "STOP",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    AudioManager.Instance.StopBGM();

                    if (SaveManager.Instance != null)
                        SaveManager.Instance.SaveBGMState("", true);
                }
                else if (audioDatabase != null &&
                        audioDatabase.TryGetBGM(
                            bgmId, out AudioClip bgmClip))
                {
                    AudioManager.Instance.PlayBGM(bgmClip);

                    if (SaveManager.Instance != null)
                        SaveManager.Instance.SaveBGMState(bgmId, false);
                }
                else
                {
                    Debug.LogWarning(
                        $"[DialogueManager] 找不到背景音乐 ID：{bgmId}");
                }
            }

            if (!string.IsNullOrWhiteSpace(record.sfx))
            {
                if (audioDatabase != null && audioDatabase.TryGetSFX(record.sfx, out AudioClip sfxClip))
                {
                    AudioManager.Instance.PlaySFX(sfxClip);
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] 找不到音效 ID：{record.sfx}");
                }
            }
        }

        private void RestoreSavedBGM()
        {
            if (SaveManager.Instance == null)
                return;

            SaveManager.SaveData save =
                SaveManager.Instance.CurrentSave;

            if (save == null)
                return;

            // 存档明确要求停止音乐
            if (save.bgmStopped)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopBGM();

                return;
            }

            // 没有记录过音乐，不进行恢复
            if (string.IsNullOrWhiteSpace(save.bgmId))
                return;

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning(
                    "[DialogueManager] 找不到 AudioManager，无法恢复背景音乐。");
                return;
            }

            if (audioDatabase != null &&
                audioDatabase.TryGetBGM(
                    save.bgmId, out AudioClip bgmClip))
            {
                AudioManager.Instance.PlayBGM(bgmClip);
                Debug.Log("[DialogueManager] 已恢复背景音乐：" + save.bgmId);
            }
            else
            {
                Debug.LogWarning(
                    "[DialogueManager] 无法恢复背景音乐，找不到 ID：" + save.bgmId);
            }
        }

        private void ShowOptionGroup()
        {
            List<DialogueRecord> options =
                dataReader.GetConsecutiveOptions(currentId);

            if (options.Count == 0)
            {
                Debug.LogError(
                    $"[DialogueManager] ID {currentId} 没有找到有效选项。");
                return;
            }

            dialogueUI.ShowOptions(options, OnOptionSelected);
        }

        private void SaveCurrentProgress()
        {
            if (SaveManager.Instance == null)
                return;

            string sceneName =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            SaveManager.Instance.SaveDialogueProgress(sceneName, currentId);
        }

        private void OnOptionSelected(DialogueRecord option)
        {
            if (isFinished)
                return;

            currentId = option.nextId;

            SaveCurrentProgress();
            ShowCurrentDialogue();
        }

        private void AdvanceDialogue()
        {
            if (!dataReader.TryGetRecord(
                    currentId, out DialogueRecord record))
                return;

            if (record.flag == "&" || record.flag == "END")
                return;

            currentId = record.nextId;

            SaveCurrentProgress();
            ShowCurrentDialogue();
        }

        private void ApplyDialogueFlags(int endedDialogueId)
        {
            if (dialogueFlags == null || FlagManager.Instance == null)
                return;

            foreach (DialogueFlagEntry entry in dialogueFlags)
            {
                if (entry == null)
                    continue;

                if (entry.endDialogueId != endedDialogueId)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.flagName))
                    continue;

                FlagManager.Instance.SetFlag(
                    entry.flagName,
                    entry.flagValue
                );

                Debug.Log(
                    $"[DialogueManager] 剧情 ID {endedDialogueId} 结束，" +
                    $"设置标记 {entry.flagName} = {entry.flagValue}"
                );
            }
        }

        private void FinishDialogue()
        {
            isFinished = true;
            isTemporaryInfo = false;
            temporaryNextStoryId = -1;

            dialogueUI.HideDialogue();

            Debug.Log("[DialogueManager] 当前剧情结束。");

            DialogueFinished?.Invoke();
        }
    }
}
