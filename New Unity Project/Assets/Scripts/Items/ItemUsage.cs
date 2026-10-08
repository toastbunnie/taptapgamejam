using UnityEngine;
using UnityEngine.Events;
using Game.Core.Interaction;

namespace Game.Items
{
    /// <summary>
    /// 需要某道具才能交互的对象：挂在门、保险箱、机关等"消耗品"型对象上。
    /// 玩家点击时检测背包里是否有指定道具，有则触发 On Used，无则触发 On Missing。
    ///
    /// 典型场景：一扇锁着的门 → 玩家点击 → 检测是否有"key_door" → 有则开门（On Used），
    /// 无则播放"门锁着"提示音（On Missing）。
    ///
    /// Inspector 字段：
    ///   - Required Item Id：需要的道具 Id
    ///   - Consume On Use：是否在成功后消耗掉道具（钥匙类 true，工具类 false）
    ///   - Prompt：悬停提示文字
    ///   - On Used：成功后触发（开门动画、音效、谜题进度）
    ///   - On Missing：缺少道具时触发（提示音/提示 UI）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemUsage : MonoBehaviour, IInteractable
    {
        [Header("配置")]
        [Tooltip("需要的道具 ItemId")]
        [SerializeField] private string _requiredItemId;

        [Tooltip("使用后是否消耗该道具（钥匙 true，工具 false）")]
        [SerializeField] private bool _consumeOnUse;

        [Tooltip("悬停提示文字")]
        [SerializeField] private string _prompt = "使用 ";

        [Header("事件")]
        [Tooltip("使用成功时触发：开门/激活机关/解谜题")]
        public UnityEvent OnUsed;

        [Tooltip("缺少道具时触发：播放'门锁着'提示")]
        public UnityEvent OnMissing;

        private bool _used;
        private bool _missingFired; // 防止 OnMissing 每帧重播（如果外部反复调 Interact）

        // ---------- IInteractable ----------
        public string PromptText
        {
            get
            {
                if (_used) return "";
                if (string.IsNullOrEmpty(_requiredItemId)) return _prompt;
                var data = ItemDatabaseRegistry.Current?.Get(_requiredItemId);
                return _prompt + (data != null ? data.itemName : _requiredItemId);
            }
        }

        public bool CanInteract => !_used && !string.IsNullOrEmpty(_requiredItemId);

        public void Interact(GameObject player)
        {
            if (_used) return;
            if (Inventory.Instance == null)
            {
                Debug.LogError("[ItemUsage] 场景中没有 Inventory 实例");
                return;
            }

            if (Inventory.Instance.Has(_requiredItemId))
            {
                _used = true;

                if (_consumeOnUse)
                {
                    Inventory.Instance.Remove(_requiredItemId);
                }

                Debug.Log($"[ItemUsage] ✓ 使用 {_requiredItemId} 成功");
                OnUsed?.Invoke();
            }
            else
            {
                Debug.Log($"[ItemUsage] ✗ 缺少道具 {_requiredItemId}");
                if (!_missingFired)
                {
                    OnMissing?.Invoke();
                    _missingFired = true;
                }
            }
        }

        // 外部主动重置（如重玩剧情）
        public void Reset()
        {
            _used = false;
            _missingFired = false;
        }
    }
}