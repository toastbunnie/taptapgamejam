using UnityEngine;
using Game.Core.Interaction;

namespace Game.Items
{
    // 场景里可拾取的道具：挂在世界中可拾取的物体上
    // 实现 IInteractable，玩家点击时自动加入背包并消失
    // Inspector 字段：
    //   - Item Id：对应 ItemData.itemId（必填）
    //   - Pickup Sound：拾取音效
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("道具配置")]
        [Tooltip("对应 ItemData.itemId，必须在 ItemDatabase 中存在")]
        [SerializeField] private string _itemId;

        [Header("拾取音效")]
        [Tooltip("留空则跳过")]
        [SerializeField] private AudioClip _pickupSound;
        [Tooltip("拾取音效音量")]
        [Range(0f, 2f)]
        [SerializeField] private float _pickupVolume = 0.8f;

        private bool _collected;

        private void Start()
        {
            // A scene reload recreates world objects, so hide pickups already
            // represented in the persistent inventory save.
            if (Inventory.Instance != null && Inventory.Instance.Has(_itemId))
            {
                _collected = true;
                gameObject.SetActive(false);
            }
        }

        // ---------- IInteractable ----------
        public string PromptText => string.Empty;
        public bool CanInteract => !_collected && !string.IsNullOrEmpty(_itemId);

        public void Interact(GameObject player)
        {
            if (_collected) return;
            if (Inventory.Instance == null)
            {
                Debug.LogError("[ItemPickup] 场景中没有 Inventory 实例");
                return;
            }

            if (Inventory.Instance.Add(_itemId))
            {
                _collected = true;

                // 拾取成功后立刻从画面中消失
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                var renderer = GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;

                // 统一走 AudioManager 的 SFX 通道，保证音量和管理逻辑一致
                if (_pickupSound != null)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(_pickupSound, _pickupVolume);
                    }
                    else
                    {
                        Debug.LogWarning("[ItemPickup] AudioManager.Instance 未初始化，跳过拾取音效");
                    }
                }

                // 物体从场景中消失，重置时再重新显示
                gameObject.SetActive(false);
            }
        }

        // ---------- 辅助 ----------
        private string GetItemName()
        {
            if (string.IsNullOrEmpty(_itemId)) return "???";
            // 通过 Inventory 拿到 Database（避免硬连接 ItemDatabase 类型）
            var db = ItemDatabaseRegistry.Current;
            var item = db?.Get(_itemId);
            return item != null ? item.itemName : _itemId;
        }

        // 用于重置场景
        public void ResetPickup()
        {
            _collected = false;
            gameObject.SetActive(true);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    // 由 Inventory.Awake() 在初始化时调用 SetDatabase
    public static class ItemDatabaseRegistry
    {
        private static ItemDatabase _current;

        public static ItemDatabase Current => _current;
        public static void SetDatabase(ItemDatabase db) => _current = db;
    }
}
