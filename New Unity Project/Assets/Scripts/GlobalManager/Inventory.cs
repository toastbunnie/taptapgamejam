using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Items.DragDrop;

namespace Game.Items
{
    // 用法：
    //   - 在 Inventory 的 Database 字段拖入 ItemDatabase.asset
    //   - 拾取道具：Inventory.Instance.Add("key_main")
    //   - 判断是否持有：Inventory.Instance.Has("key_main")
    //   - 监听获得事件：在 Inspector 拖 OnItemAdded 的 UnityEvent
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        [Tooltip("道具数据库（全局唯一）")]
        [SerializeField] private ItemDatabase _database;

        private readonly HashSet<string> _ownedIds = new HashSet<string>();

        [System.Serializable] public class ItemDataEvent : UnityEvent<ItemData> { }
        [System.Serializable] public class StringEvent : UnityEvent<string> { }

        // 获得道具时触发（参数：ItemData）
        public ItemDataEvent OnItemAdded;

        // 移除道具时触发（参数：itemId）
        public StringEvent OnItemRemoved;

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

        // 获得道具。
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

            return AddInternal(data);
        }

        // 直接通过 ItemData 添加（用于调酒等系统生成的新道具，无需先注册到 database）
        public bool Add(ItemData data)
        {
            if (data == null)
            {
                Debug.LogError("[Inventory] 传入的 ItemData 为空");
                return false;
            }

            if (_ownedIds.Contains(data.itemId))
            {
                Debug.Log($"[Inventory] 已拥有 {data.itemId}，跳过");
                return false;
            }

            return AddInternal(data);
        }

        private bool AddInternal(ItemData data)
        {
            _ownedIds.Add(data.itemId);
            Debug.Log($"[Inventory] ✨ 获得道具: {data.itemName}");
            OnItemAdded?.Invoke(data);
            DragDropRuleManager.ShowAcquisitionFeedback(
                $"获得道具：{data.itemName}");
            return true;
        }

        // 判断是否已持有
        public bool Has(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _ownedIds.Contains(itemId);
        }

        // 移除道具（触发 OnItemRemoved 事件，UI 可监听）
        public bool Remove(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (!_ownedIds.Remove(itemId)) return false;

            Debug.Log($"[Inventory] 🗑 移除道具: {itemId}");
            OnItemRemoved?.Invoke(itemId);
            return true;
        }

        // 清空（重置游戏用），并通知背包 UI 清理已显示的槽位。
        public void Clear()
        {
            if (_ownedIds.Count == 0)
                return;

            string[] removedIds = new string[_ownedIds.Count];
            _ownedIds.CopyTo(removedIds);
            _ownedIds.Clear();

            foreach (string itemId in removedIds)
                OnItemRemoved?.Invoke(itemId);
        }

        // 当前持有的所有 itemId（只读）
        public IReadOnlyCollection<string> AllOwnedIds => _ownedIds;
    }
}
