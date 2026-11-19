using UnityEngine;
using Game.Core.Interaction;

public class PlayerInteraction : MonoBehaviour
{
    [Header("点击设置")]
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Only modal settings/history panels should block scene interactions.
        // A global EventSystem pointer check can get stuck on unrelated or
        // invisible graphics and prevent all later scene clicks.
        if (Game.UI.SettingsPanel.IsBlockingSceneInput)
            return;

        // 特写面板打开期间冻结场景点击，防止点击穿透到背景物体。
        if (SinkCloseupInteractable.IsAnyOpen ||
            CloseupPanelInteractable.IsAnyOpen)
            return;

        // 对话或手记显示期间，不允许点击场景物品。
        // 对话推进/手记操作由对应 UI 自己处理。
        Game.Dialogue.DialogueManager dialogueManager =
            FindObjectOfType<Game.Dialogue.DialogueManager>();

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
            return;

        JournalUI journalUI = FindObjectOfType<JournalUI>();
        if (journalUI != null && journalUI.IsOpen)
            return;

        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerInteraction：没有找到主摄像机！");
            return;
        }

        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider == null)
        {
            return;
        }

        // 同一物体同时配置情绪触发器和其他交互时，先消费一次情绪触发。
        // 情绪触发器完成后（CanInteract=false），后续点击继续走原交互。
        SceneEmotionTrigger emotionTrigger =
            hit.collider.GetComponent<SceneEmotionTrigger>();
        if (emotionTrigger != null && emotionTrigger.CanInteract)
        {
            emotionTrigger.Interact(gameObject);
            return;
        }

        IInteractable interactable = hit.collider.GetComponent<IInteractable>();

        if (interactable == null)
        {
            return;
        }

        if (!interactable.CanInteract)
        {
            return;
        }

        interactable.Interact(gameObject);
    }
}
