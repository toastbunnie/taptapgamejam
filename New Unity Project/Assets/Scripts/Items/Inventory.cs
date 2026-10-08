using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Items
{
    /// <summary>
    /// 玩家背包：场景里挂一个全局唯一实例（通常和 GameFlow 一起挂在 GameManager 上）。
    ///
    /// 用法：
    ///   - 在 Inventory 的 Database 字段拖入 ItemDatabase.asset。
    ///   - 拾取道具：Inventory.Instance.Add("key_main")
    ///   - 判断是否持有：Inventory.Instance.Has("key_main")
    ///   - 监听获得事件：在 Inspector 拖 OnItemAdded 的 UnityEvent
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        [Tooltip("道具数据库 SO（全局唯一）")]
        [SerializeField] private ItemDatabase _database;

        private readonly HashSet<string> _ownedIds = new HashSet<string>();

        [System.Serializable] public class ItemDataEvent : UnityEvent<ItemData> { }

        /// <summary>获得道具时触发（参数：ItemData）</summary>
        public ItemDataEvent OnItemAdded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Inventory] 已存在另一个 Inventory 实例，销毁本对象");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_database != null)
            {
                _database.Init();
                ItemDatabaseRegistry.SetDatabase(_database);
            }
            else
            {
                Debug.LogError("[Inventory] 未设置 ItemDatabase！道具系统将无法工作");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 获得道具。
        /// </summary>
        public bool Add(string itemId)
        {
            if (_database == null)
            {
                Debug.LogError("[Inventory] 数据库为空");
                return false;
            }

            if (_ownedIds.Contains(itemId))
            {
                Debug.Log($"[Inventory] 已拥有 {itemId}，跳过");
                return false;
            }

            var data = _database.Get(itemId);
            if (data == null)
            {
                Debug.LogError($"[Inventory] 找不到 itemId='{itemId}' 的 ItemData");
                return false;
            }

            _ownedIds.Add(itemId);
            Debug.Log($"[Inventory] ✨ 获得道具: {data.itemName}");
            OnItemAdded?.Invoke(data);
            return true;
        }

        /// <summary>判断是否已持有</summary>
        public bool Has(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _ownedIds.Contains(itemId);
        }

        /// <summary>移除道具（未来用得到）</summary>
        public bool Remove(string itemId)
        {
            return _ownedIds.Remove(itemId);
        }

        /// <summary>清空（重置游戏用）</summary>
        public void Clear() => _ownedIds.Clear();

        /// <summary>当前持有的所有 itemId（只读）</summary>
        public IReadOnlyCollection<string> AllOwnedIds => _ownedIds;
    }
}