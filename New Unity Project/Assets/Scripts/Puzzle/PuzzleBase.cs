using UnityEngine;
using UnityEngine.Events;
using Game.Core.Interaction;

namespace Game.Puzzle
{
    /// <summary>
    /// 谜题基类：所有具体谜题（密码箱、拼图、机关、组合锁等）继承这个类。
    ///
    /// 实现模式：
    ///   1. 简单谜题：直接重写 OnPlayerInteract()，在内部检测条件，达成后调 Solve()。
    ///   2. 复杂谜题：配合 IPuzzleCondition，让外部 Poll IsMet()，或用事件触发 Solve()。
    ///
    /// Inspector 字段：
    ///   - Puzzle Name：谜题名称（用于日志）
    ///   - Prompt：悬停提示文字
    ///   - Is Solved On Start：是否启动时就是已解决状态（用于跳关调试）
    ///   - On Solved：解谜成功事件（UIManager / AudioManager 监听）
    ///   - On Interact：玩家点击时事件（去开 UI 面板）
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour, IInteractable
    {
        [Header("基础")]
        [Tooltip("谜题名称")]
        [SerializeField] protected string _puzzleName = "未命名谜题";

        [Tooltip("悬停提示文字")]
        [SerializeField] protected string _prompt = "解谜";

        [Header("调试")]
        [Tooltip("启动时是否已解（用于跳关）")]
        [SerializeField] private bool _isSolvedOnStart;

        [Header("事件")]
        [Tooltip("解谜成功时触发。 UI 和 AudioManager 都接这里")]
        public UnityEvent OnSolved;

        [Tooltip("玩家点击时触发。 UIManager 可挂上'显示谜题面板'")]
        public UnityEvent OnInteract;

        /// <summary>是否已解</summary>
        public bool IsSolved { get; private set; }

        // ---------- IInteractable ----------
        public string PromptText => IsSolved ? "" : _prompt;
        public bool CanInteract => !IsSolved;

        protected virtual void Start()
        {
            if (_isSolvedOnStart) Solve();
        }

        public void Interact(GameObject player)
        {
            if (IsSolved) return;
            OnInteract?.Invoke();   // 触发外部逻辑（开 UI 等）
            OnPlayerInteract(player); // 触发子类逻辑
        }

        /// <summary>子类重写：玩家点击时具体做什么</summary>
        protected abstract void OnPlayerInteract(GameObject player);

        /// <summary>子类在条件满足时调这个，标记谜题已解</summary>
        protected void Solve()
        {
            if (IsSolved) return;
            IsSolved = true;
            Debug.Log($"[Puzzle:{_puzzleName}] ✅ 已解");
            OnSolved?.Invoke();
        }

        /// <summary>外部强制重置（如重玩剧情）</summary>
        public virtual void Reset()
        {
            IsSolved = false;
        }
    }
}