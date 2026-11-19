using UnityEngine;
using Game.Items;

namespace Game.Dialogue
{
    /// <summary>Grants one inventory item when a configured dialogue line begins.</summary>
    [RequireComponent(typeof(DialogueManager))]
    public sealed class DialogueItemReward : MonoBehaviour
    {
        [SerializeField, Min(1)] private int dialogueId = 1;
        [SerializeField] private string itemId;

        private DialogueManager dialogueManager;
        private bool granted;

        private void Awake()
        {
            dialogueManager = GetComponent<DialogueManager>();
        }

        private void OnEnable()
        {
            if (dialogueManager == null)
                dialogueManager = GetComponent<DialogueManager>();

            if (dialogueManager != null)
                dialogueManager.DialogueRecordStarted += OnDialogueRecordStarted;
        }

        private void OnDisable()
        {
            if (dialogueManager != null)
                dialogueManager.DialogueRecordStarted -= OnDialogueRecordStarted;
        }

        private void OnDialogueRecordStarted(int startedId)
        {
            if (granted || startedId != dialogueId)
                return;

            if (Inventory.Instance == null)
            {
                Debug.LogError($"[{nameof(DialogueItemReward)}] 找不到 Inventory，无法发放道具 '{itemId}'。", this);
                return;
            }

            if (Inventory.Instance.Add(itemId))
                granted = true;
            else if (Inventory.Instance.Has(itemId))
                granted = true;
        }
    }
}
