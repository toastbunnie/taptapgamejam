using UnityEngine;
using UnityEngine.Events;
using Game.Puzzle;

namespace Game.Sample
{
    /// <summary>
    /// 示例谜题：密码箱。
    ///
    /// 工作流程：
    ///   1. 玩家点击密码箱 → OnInteract 触发（UIManager 弹出密码输入 UI）
    ///   2. UI 输入完成后回调 OnCodeEntered(code)
    ///   3. 如果 code == correctCode → Solve() 触发 → GameFlow 收到胜利信号
    /// </summary>
    public class PasswordBox : PuzzleBase
    {
        [Header("密码设置")]
        [Tooltip("正确密码")]
        [SerializeField] private string _correctCode = "1234";

        private string _inputCode = "";
        [SerializeField] private GameObject _errorText;

        [Header("UI 回调（给 UIManager 用）")]
        [Tooltip("UI 输入完成时调用，传入玩家输入的密码")]
        public UnityEvent<string> OnCodeSubmitted;

        protected override void OnPlayerInteract(GameObject player)
        {
            // 玩家点击时，触发 OnInteract 事件（UIManager 监听到后会显示密码输入面板）
            // UIManager 在收到显示请求后，调 OnCodeSubmitted(code) 传回玩家输入
            Debug.Log($"[{_puzzleName}] 等待玩家输入密码…");

            UIManager.Instance.OpenPanel("PasswordPanel");
        }

        /// <summary>
        /// 由 UIManager 或其他系统调用：传入玩家输入的密码。
        /// </summary>
        public void OnCodeSubmit(string inputCode = null)
        {
            _inputCode = inputCode ?? _inputCode;

            if (string.IsNullOrEmpty(_inputCode))
            {
                Debug.Log($"[{_puzzleName}] 还未输入密码");
                return;
            }

            OnCodeSubmitted?.Invoke(_inputCode);

            if (_inputCode == _correctCode)
            {
                if (_errorText != null)
                    _errorText.SetActive(false);

                Solve();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ClosePanel("PasswordPanel");
                }
            }
            else
            {
                Debug.Log($"[{_puzzleName}] 密码错误：{_inputCode}");

                if (_errorText != null)
                    _errorText.SetActive(true);

                _inputCode = "";
            }
        }

        public override void Reset()
        {
            base.Reset();
            _inputCode = "";
        }
    }
}