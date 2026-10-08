using UnityEngine;

namespace Game.Items
{
    /// <summary>道具类型</summary>
    public enum ItemType
    {
        Key,         // 钥匙类（用于 ItemUsage 解锁）
        Tool,        // 工具（螺丝刀、撬棍）
        Document,    // 文档/线索
        Consumable   // 消耗品
    }

    /// <summary>
    /// 单个道具的数据定义（ScriptableObject）。
    /// 在 Project 面板右键 → Create → Game → Item 创建。
    /// 每个道具是一个 .asset 文件，策划/美术可以独立编辑，不动代码。
    ///
    /// 字段说明：
    ///   - itemId：程序内部使用的唯一 ID（如 "key_main"），必须全局唯一
    ///   - itemName：UI 显示名（如 "生锈的钥匙"）
    ///   - icon：UI 背包格子里的图标
    ///   - type：道具类型，未来做背包 UI 时可按类型分组
    ///   - worldPrefab：场景中怎么显示（可选，留空则用 ItemPickup 自己的模型）
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Game/Item", order = 0)]
    public class ItemData : ScriptableObject
    {
        [Tooltip("程序内部使用的唯一 ID，必须全局唯一")]
        public string itemId;

        [Tooltip("UI 显示名")]
        public string itemName = "未命名道具";

        [Tooltip("UI 显示描述")]
        [TextArea(2, 4)]
        public string description = "";

        [Tooltip("UI 背包格子图标")]
        public Sprite icon;

        [Tooltip("道具类型")]
        public ItemType type = ItemType.Key;

        [Tooltip("场景中可拾取物体的预制体（可选）")]
        public GameObject worldPrefab;
    }
}