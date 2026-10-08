using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// 谜题成功条件接口（可选）。
    /// 简单的谜题（如密码箱、开关类）直接继承 PuzzleBase 并重写 OnPlayerInteract 即可。
    /// 复杂条件（如"必须在房间 A、B、C 都按过按钮"）可实现这个接口，由 PuzzleManager 调用 IsMet()。
    ///
    /// 用法示例：
    ///   public class MultiButtonCondition : MonoBehaviour, IPuzzleCondition
    ///   {
    ///       public List<Button> buttons;
    ///       public bool IsMet() => buttons.All(b => b.IsPressed);
    ///   }
    /// </summary>
    public interface IPuzzleCondition
    {
        /// <summary>当前谜题是否满足解决条件</summary>
        bool IsMet();

        /// <summary>（可选）当前进度描述，用于 UI 提示</summary>
        string ProgressHint { get; }
    }
}