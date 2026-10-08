using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// 道具总数据库：所有 ItemData 的注册中心。
    /// 在 Project 面板右键 → Create → Game → ItemDatabase 创建（全局一份）。
    ///
    /// 用法：
    ///   1. 在 Inventory 组件的 Database 字段指向本 SO。
    ///   2. Inventory.Awake() 会自动调 Init()，无需手动调用。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/ItemDatabase", order = 1)]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("所有道具数据，按 itemId 唯一索引")]
        public List<ItemData> allItems = new List<ItemData>();

        private Dictionary<string, ItemData> _lookup;

        /// <summary>
        /// 构建 itemId → ItemData 的字典索引。
        /// 由 Inventory.Awake() 调用，避免重复构建。
        /// </summary>
        public void Init()
        {
            _lookup = new Dictionary<string, ItemData>();
            foreach (var item in allItems)
            {
                if (item == null)
                {
                    Debug.LogWarning("[ItemDatabase] allItems 里有 null 槽位，跳过");
                    continue;
                }
                if (string.IsNullOrEmpty(item.itemId))
                {
                    Debug.LogWarning($"[ItemDatabase] {item.name} 缺少 itemId，跳过");
                    continue;
                }
                if (_lookup.ContainsKey(item.itemId))
                {
                    Debug.LogError($"[ItemDatabase] itemId '{item.itemId}' 重复！请检查");
                    continue;
                }
                _lookup[item.itemId] = item;
            }
            Debug.Log($"[ItemDatabase] 初始化完成，共 {_lookup.Count} 个道具");
        }

        /// <summary>
        /// 通过 itemId 查询 ItemData。
        /// </summary>
        public ItemData Get(string itemId)
        {
            if (_lookup == null)
            {
                Debug.LogError("[ItemDatabase] 未初始化！请确认场景里有 Inventory 组件");
                return null;
            }
            _lookup.TryGetValue(itemId, out var data);
            return data;
        }

        /// <summary>
        /// 运行时新增道具（一般用于测试，正式流程用 Inspector 拖入）。
        /// </summary>
        public void Register(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.itemId)) return;
            if (_lookup == null) Init();
            if (!_lookup.ContainsKey(data.itemId))
            {
                _lookup[data.itemId] = data;
                allItems.Add(data);
            }
        }
    }
}