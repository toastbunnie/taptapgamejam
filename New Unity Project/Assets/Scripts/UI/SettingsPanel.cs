using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game.UI
{
    /// <summary>
    /// 设置面板 UI 控制。每个场景一个。
    /// 用 CanvasGroup 做淡入淡出。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Slider _soundSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _voiceSlider;
        [SerializeField] private Button _closeButton;       // 左上"返回"
        [SerializeField] private Button _reloadButton;      // 右下"重载当前环境"

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCo;
        private bool _isOpen = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvasGroup = GetComponent<CanvasGroup>();
            // 不要 SetActive(false)！会禁掉 Update，ESC 监听失效
            // 只设 alpha=0、关交互，面板仍在场景中（不可见不可点）
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        void Start()
        {
            // 1. 初始化 slider 值为 SettingsManager 当前值
            if (SettingsManager.Instance != null)
            {
                if (_soundSlider != null) _soundSlider.value = SettingsManager.Instance.SoundVolume;
                if (_musicSlider != null) _musicSlider.value = SettingsManager.Instance.MusicVolume;
                if (_voiceSlider != null) _voiceSlider.value = SettingsManager.Instance.VoiceVolume;
            }

            // 2. 绑定 slider 拖动事件（实时改音量）
            if (_soundSlider != null) _soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (_voiceSlider != null) _voiceSlider.onValueChanged.AddListener(OnVoiceChanged);

            // 3. 绑定按钮事件
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_reloadButton != null) _reloadButton.onClick.AddListener(OnReloadClicked);
        }

        void Update()
        {
            // ESC 切换
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isOpen) Hide();
                else Show();
            }
        }

        // ========== 公共方法（外部按钮调用）==========

        public void Show()
        {
            if (_isOpen) return;
            if (!UIPanelExclusivity.TryOpen(UIPanelExclusivity.Panel.Settings))
                return;

            _isOpen = true;
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            gameObject.SetActive(true);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _fadeCo = StartCoroutine(FadeCanvas(0f, 1f, _fadeDuration));
        }

        public void Hide()
        {
            Hide(immediate: false);
        }

        public void Hide(bool immediate)
        {
            if (!_isOpen && !immediate) return;
            _isOpen = false;
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            if (immediate)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                // 不调 SetActive(false)！会禁掉 Update，ESC 监听失效
                return;
            }
            _fadeCo = StartCoroutine(FadeCanvas(1f, 0f, _fadeDuration, onDone: () =>
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                // 同上，保持 GameObject active，让 Update 继续监听 ESC
            }));
        }

        // ========== Slider 回调（拖动时实时改 + 存盘）==========

        void OnSoundChanged(float v)
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetSoundVolume(v);
                SettingsManager.Instance.Save();
            }
        }

        void OnMusicChanged(float v)
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetMusicVolume(v);
                SettingsManager.Instance.Save();
            }
        }

        void OnVoiceChanged(float v)
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetVoiceVolume(v);
                SettingsManager.Instance.Save();
            }
        }

        // 拖完 slider（PointerUp）才存盘
        public void OnSliderPointerUp()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.Save();
        }

        // ========== 按钮回调 ==========

        void OnReloadClicked()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.ReloadCurrentEnvironment();
        }

        // ========== 动画 ==========

        IEnumerator FadeCanvas(float from, float to, float duration, System.Action onDone = null)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;  // 不受 timeScale 影响
                _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
            onDone?.Invoke();
        }
    }
}
