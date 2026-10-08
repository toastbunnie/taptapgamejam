using UnityEngine;
using UnityEngine.Events;
using Game.Core.Interaction;

namespace Game.Items
{
    /// <summary>
    /// 场景里可拾取的道具：挂在世界中可拾取物体的 prefab 上（钥匙、文档等）。
    /// 实现 IInteractable，玩家点击时自动加入背包并消失。
    ///
    /// Inspector 字段：
    ///   - Item Id：对应 ItemData.itemId（必填）
    ///   - Prompt：悬停时显示的前缀文字（默认"拾取 "）
    ///   - Pickup Sound：拾取音效（可选，留空用 AudioManager 自动派）
    ///   - On Picked Up：拾取成功后的额外事件（给 AudioManager / UIManager 挂）
    ///   - Keep Visible After Pickup：保留显示（用于需要"已读"标记的文档类）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("道具配置")]
        [Tooltip("对应 ItemData.itemId，必须在 ItemDatabase 中存在")]
        [SerializeField] private string _itemId;

        [Tooltip("悬停时显示的前缀文字，如'拾取 '")]
        [SerializeField] private string _prompt = "拾取 ";

        [Header("音效（可选）")]
        [Tooltip("拾取音效，留空则跳过。可挂给 AudioManager 接管")]
        [SerializeField] private AudioClip _pickupSound;

        [Header("事件")]
        [Tooltip("拾取成功时触发。兔汁可在此挂 AudioManager.PlaySFX()")]
        public UnityEvent OnPickedUp;

        [Header("高级")]
        [Tooltip("拾取后保留显示（用于文档类需要'已读'标记）")]
        [SerializeField] private bool _keepVisibleAfterPickup;

        private bool _collected;

        // ---------- IInteractable ----------
        public string PromptText => _collected ? "" : _prompt + GetItemName();
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
                OnPickedUp?.Invoke();

                // 直接播音效（如果兔汁的 AudioManager 没接，这里能 fallback）
                if (_pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(_pickupSound, transform.position);
                }

                if (!_keepVisibleAfterPickup)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    // 保留可见时禁用碰撞（不能再点）
                    var col = GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
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

        /// <summary>
        /// 用于重置场景（编辑器调试用）。
        /// </summary>
        public void ResetPickup()
        {
            _collected = false;
            gameObject.SetActive(true);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    /// <summary>
    /// ItemDatabase 的全局注册表（避免 ItemPickup 与 Inventory 直接耦合）。
    /// 由 Inventory.Awake() 在初始化时调用 SetDatabase。
    /// 这样 ItemPickup 只需要关心 itemId → ItemData 的查询。
    /// </summary>
    public static class ItemDatabaseRegistry
    {
        private static ItemDatabase _current;

        public static ItemDatabase Current => _current;
        public static void SetDatabase(ItemDatabase db) => _current = db;
    }
}