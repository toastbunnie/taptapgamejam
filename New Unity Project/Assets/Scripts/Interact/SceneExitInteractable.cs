
using UnityEngine;
using Game.Core.Interaction;
using Game.Core.SceneTransition;
using Game.Items;

public class SceneExitInteractable : MonoBehaviour, IInteractable
{
    [Header("出口设置")]
    [SerializeField] private string destinationSceneName;

    [Header("交互提示")]
    [SerializeField] private string promptText = "离开这里";

    [Header("剧情解锁条件")]
    [SerializeField] private bool requireFlag = false;
    [SerializeField] private string requiredFlagName = "";
    [SerializeField] private bool requiredFlagValue = true;

    [Header("道具解锁条件")]
    [SerializeField] private bool requireItem = false;
    [SerializeField] private string requiredItemId = "";

    public string PromptText => promptText;

    public bool CanInteract
    {
        get
        {
            if (requireFlag &&
                (FlagManager.Instance == null ||
                 FlagManager.Instance.GetFlag(requiredFlagName) != requiredFlagValue))
                return false;

            if (requireItem &&
                (Inventory.Instance == null ||
                 !Inventory.Instance.Has(requiredItemId)))
                return false;

            return true;
        }
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract)
            return;

        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogError("出口没有设置目标场景名称！", this);
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("找不到 SceneTransitionManager！", this);
            return;
        }

        SceneTransitionManager.Instance.FadeToScene(
            destinationSceneName
        );
    }
}
