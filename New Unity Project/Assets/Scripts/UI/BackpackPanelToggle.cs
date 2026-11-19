using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class BackpackPanelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private Image buttonImage;
        [Tooltip("背包关闭时显示的按钮图片（点击后打开背包）")]
        [SerializeField] private Sprite openPanelSprite;
        [Tooltip("背包打开时显示的按钮图片（点击后关闭背包）")]
        [SerializeField] private Sprite closePanelSprite;

        private void Awake()
        {
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            RefreshButtonImage();
        }

        public void Toggle()
        {
            if (backpackPanel == null)
                return;

            bool willOpen = !backpackPanel.activeSelf;
            if (willOpen &&
                !UIPanelExclusivity.TryOpen(UIPanelExclusivity.Panel.Backpack))
            {
                return;
            }

            backpackPanel.SetActive(willOpen);
            RefreshButtonImage();

            if (willOpen && Game.UI.InventoryUI.Instance != null)
            {
                Game.UI.InventoryUI.Instance.RefreshFromInventory();
            }
        }

        public void Close()
        {
            if (backpackPanel != null)
                backpackPanel.SetActive(false);

            RefreshButtonImage();
        }

        private void RefreshButtonImage()
        {
            if (buttonImage == null || backpackPanel == null)
                return;

            Sprite stateSprite = backpackPanel.activeSelf
                ? closePanelSprite
                : openPanelSprite;

            if (stateSprite != null)
                buttonImage.sprite = stateSprite;
        }
    }
}
