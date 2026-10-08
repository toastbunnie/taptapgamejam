using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Core;

namespace Game.Puzzle
{
    /// <summary>
    /// 谜题管理器：收集场景里所有 PuzzleBase，统计已解数量。
    /// 当所有谜题都解了，调用 GameFlow.OnAllPuzzlesSolved() 触发胜利。
    ///
    /// 用法：
    ///   - 场景里挂一个空对象 PuzzleManager，加这个组件。
    ///   - 在 All Puzzles 数组里拖入所有 PuzzleBase（密码箱、拼图、机关等）。
    ///   - 也可以让 PuzzleBase 自己 Start() 时调 PuzzleManager.Register()，那样不用手动拖。
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [Header("所有谜题")]
        [Tooltip("场景里所有谜题。可以手动拖入，也可以靠 PuzzleBase.Start() 自动注册")]
        [SerializeField] private List<PuzzleBase> _allPuzzles = new List<PuzzleBase>();

        [Header("事件")]
        [Tooltip("每解开一个谜题触发。参数：(已解数量, 总数)")]
        public UnityEvent<int, int> OnPuzzleProgress;

        private int _solvedCount;
        private readonly HashSet<PuzzleBase> _registered = new HashSet<PuzzleBase>();

        public int TotalCount => _allPuzzles.Count;
        public int SolvedCount => _solvedCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Start 阶段：所有 PuzzleBase 已就绪，订阅 OnSolved
            foreach (var puzzle in _allPuzzles)
            {
                if (puzzle != null) RegisterPuzzle(puzzle);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// PuzzleBase 在 Awake/Start 时调一下，自动注册到管理器。
        /// </summary>
        public void RegisterPuzzle(PuzzleBase puzzle)
        {
            if (puzzle == null || _registered.Contains(puzzle)) return;
            _registered.Add(puzzle);
            puzzle.OnSolved.AddListener(() => OnPuzzleSolved(puzzle));

            if (!_allPuzzles.Contains(puzzle)) _allPuzzles.Add(puzzle);

            // 如果启动时已经是已解决状态（_isSolvedOnStart），把它计入已解
            if (puzzle.IsSolved) OnPuzzleSolved(puzzle);
        }

        private void OnPuzzleSolved(PuzzleBase puzzle)
        {
            // 防止重复计数：_solvedCount 只在每次新事件触发时 +1，但 PuzzleBase 的 Solve() 内部有 IsSolved 保护。
            // 用 HashSet 去重
            if (_solvedSet.Contains(puzzle)) return;
            _solvedSet.Add(puzzle);

            _solvedCount = _solvedSet.Count;
            Debug.Log($"[PuzzleManager] 进度: {_solvedCount}/{_allPuzzles.Count}");

            OnPuzzleProgress?.Invoke(_solvedCount, _allPuzzles.Count);

            if (_solvedCount >= _allPuzzles.Count && _allPuzzles.Count > 0)
            {
                if (GameFlow.Instance != null)
                {
                    GameFlow.Instance.OnAllPuzzlesSolved();
                }
                else
                {
                    Debug.LogError("[PuzzleManager] GameFlow.Instance 为空！请确认场景里有 GameFlow");
                }
            }
        }

        private readonly HashSet<PuzzleBase> _solvedSet = new HashSet<PuzzleBase>();
    }
}