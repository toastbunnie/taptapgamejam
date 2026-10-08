using UnityEngine;

namespace Game.Core.Interaction
{
    /// <summary>
    /// 所有可交互物体的统一接口。
    /// "通用点击系统"在玩家点击时会调用 Interact()，
    /// 任何想被点击的物体只要实现这个接口即可。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 鼠标悬停时 UI 上显示的提示文字。
        /// 例："拾取钥匙" / "打开保险箱" / "使用钥匙"。
        /// 已不可交互时返回空字符串，UI 不显示。
        /// </summary>
        string PromptText { get; }

        /// <summary>
        /// 当前是否允许交互。
        /// 例：已拾取的道具返回 false（不能重复拾取）。
        /// 例：已解谜题返回 false（不能再点）。
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// 点击触发。具体表现由实现者决定：
        /// - ItemPickup：加入背包并消失
        /// - PuzzleBase：触发谜题 UI / 检测解谜条件
        /// - ItemUsage：检测是否持有道具
        /// </summary>
        /// <param name="player">发起交互的玩家 GameObject（一般用不上，但保留以备扩展）</param>
        void Interact(GameObject player);
    }
}