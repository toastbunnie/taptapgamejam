using UnityEngine;
using UnityEngine.UI;
using Game.Core.Interaction;
using Game.Items;
using Game.Items.DragDrop;
using Game.Dialogue;

/// <summary>
/// Shows the existing close-up panel assigned in the Inspector when the sink is clicked.
/// No UI is generated at runtime; the assigned panel also serves as the inventory drop target.
/// </summary>
[RequireComponent(typeof(DropZone))]
public sealed class SinkCloseupInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "查看水槽";
    [SerializeField] private GameObject closeupPanel;
    [SerializeField] private int introDialogueId = 58;

    private static SinkCloseupInteractable openInstance;
    private DropZone dropZone;
    private SinkCloseupDropTarget dropTarget;
    private DialogueManager dialogueManager;
    private bool introPlayed;
    private bool waitingForIntroDialogue;

    public static bool IsAnyOpen =>
        openInstance != null && openInstance.closeupPanel != null && openInstance.closeupPanel.activeSelf;

    public string PromptText => promptText;
    public bool CanInteract => closeupPanel != null;

    private void Awake()
    {
        dropZone = GetComponent<DropZone>();
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
            dialogueManager.DialogueFinished += OnIntroDialogueFinished;
        // Keep the collider for clicking the sink, but reject drag-and-drop
        // until its close-up panel is open.
        dropZone.SetDropEnabled(false);

        if (closeupPanel == null)
        {
            Debug.LogError("[SinkCloseup] 请在 Inspector 指定场景中已有的特写面板。", this);
            return;
        }

        closeupPanel.SetActive(false);
        dropTarget = closeupPanel.GetComponent<SinkCloseupDropTarget>();
        if (dropTarget == null)
            dropTarget = closeupPanel.AddComponent<SinkCloseupDropTarget>();
        dropTarget.Initialize(this, dropZone);

        // Existing state images cover the close-up panel. Let their UI graphics
        // receive inventory drop raycasts while the panel is visible.
        SetPanelImagesRaycastTarget(false);
    }

    private void OnDestroy()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueFinished -= OnIntroDialogueFinished;

        if (openInstance == this)
            openInstance = null;
    }

    public void Interact(GameObject player) => OpenCloseup();

    /// <summary>Can also be assigned to a Button's OnClick event.</summary>
    public void OpenCloseup()
    {
        if (closeupPanel == null)
            return;

        if (openInstance != null && openInstance != this)
            openInstance.CloseCloseup();

        openInstance = this;
        closeupPanel.SetActive(true);
        SetPanelImagesRaycastTarget(true);

        if (!introPlayed && introDialogueId > 0 && dialogueManager != null)
        {
            dropZone.SetDropEnabled(false);
            waitingForIntroDialogue = true;
            dialogueManager.StartMainDialogue(introDialogueId);

            // If the configured ID could not start, do not leave the puzzle locked.
            if (!dialogueManager.IsDialogueActive)
            {
                waitingForIntroDialogue = false;
                introPlayed = true;
                dropZone.SetDropEnabled(true);
            }
        }
        else
        {
            dropZone.SetDropEnabled(true);
        }
    }

    /// <summary>Can also be assigned to a Button's OnClick event.</summary>
    public void CloseCloseup()
    {
        if (closeupPanel != null)
        {
            SetPanelImagesRaycastTarget(false);
            closeupPanel.SetActive(false);
        }

        if (dropZone != null)
            dropZone.SetDropEnabled(false);

        if (openInstance == this)
            openInstance = null;
    }

    private void Update()
    {
        if (IsAnyOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseCloseup();
    }

    private void SetPanelImagesRaycastTarget(bool enabled)
    {
        if (closeupPanel == null)
            return;

        foreach (Image image in closeupPanel.GetComponentsInChildren<Image>(true))
            image.raycastTarget = enabled;
    }

    internal void OnSuccessfulDrop()
    {
        // Keep the existing close-up visible so dialogue can transition its before/after images.
    }

    private void OnIntroDialogueFinished()
    {
        if (!waitingForIntroDialogue)
            return;

        waitingForIntroDialogue = false;
        introPlayed = true;

        if (closeupPanel != null && closeupPanel.activeSelf && dropZone != null)
            dropZone.SetDropEnabled(true);
    }
}

public sealed class SinkCloseupDropTarget : MonoBehaviour
{
    private SinkCloseupInteractable owner;
    private DropZone zone;

    public void Initialize(SinkCloseupInteractable closeup, DropZone dropZone)
    {
        owner = closeup;
        zone = dropZone;
    }

    public bool TryDrop(string itemId)
    {
        if (!SinkCloseupInteractable.IsAnyOpen ||
            zone == null ||
            !zone.AcceptsItem(itemId) ||
            Inventory.Instance == null ||
            !Inventory.Instance.Has(itemId))
        {
            return false;
        }

        if (!zone.RegisterInventoryDrop(itemId))
            return false;

        if (!Inventory.Instance.Remove(itemId))
            return false;

        Game.UI.BackpackPanelToggle backpackToggle =
            FindObjectOfType<Game.UI.BackpackPanelToggle>();
        if (backpackToggle != null)
            backpackToggle.Close();

        owner?.OnSuccessfulDrop();
        return true;
    }
}
