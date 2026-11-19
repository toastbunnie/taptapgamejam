using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core.SceneTransition
{
    // 场景切换管理器：黑屏淡入淡出切换场景。
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [Header("淡入淡出设置")]
        [Tooltip("单次淡入/淡出的秒数")]
        [SerializeField] private float fadeDuration = 1f;

        [Tooltip("覆盖色（默认黑）")]
        [SerializeField] private Color fadeColor = Color.black;

        private Canvas _canvas;
        private Image _fadeImage;
        private bool _isFading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateFadeUI();
            // 启动时透明（不遮挡首场景）
            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void CreateFadeUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // 最顶层

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(transform, false);
            _fadeImage = imageObj.AddComponent<Image>();
            _fadeImage.color = fadeColor;
            _fadeImage.raycastTarget = false; // 不阻挡 UI 点击

            var rt = imageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        // 淡出到黑屏，然后加载新场景，再淡入回来
        public void FadeToScene(string sceneName)
        {
            StartExclusive(FadeToSceneRoutine(sceneName));
        }

        // 单独淡出到黑屏（覆盖屏幕）
        public void FadeIn(Action onComplete = null)
        {
            StartExclusive(FadeRoutine(1f, onComplete));
        }

        // 单独淡入回来（变透明看到场景）
        public void FadeOut(Action onComplete = null)
        {
            StartExclusive(FadeRoutine(0f, onComplete));
        }

        public bool IsFading => _isFading;

        private void StartExclusive(IEnumerator routine)
        {
            if (_isFading)
                return;

            // Lock synchronously so another request in the same frame cannot overlap.
            _isFading = true;
            StartCoroutine(ExclusiveRoutine(routine));
        }

        private IEnumerator ExclusiveRoutine(IEnumerator routine)
        {
            try
            {
                yield return StartCoroutine(routine);
            }
            finally
            {
                _isFading = false;
            }
        }

        private IEnumerator FadeToSceneRoutine(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneTransition] 加载失败：场景 '{sceneName}' 不在 Build Settings 或拼错！");
                yield break;
            }

            yield return FadeRoutine(1f, null);

            AsyncOperation op = null;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[SceneTransition] 加载场景 '{sceneName}' 时发生异常：{e.Message}");
            }

            if (op == null)
            {
                Debug.LogError($"[SceneTransition] 加载失败：场景 '{sceneName}' 不在 Build Settings 或拼错！");
                // Keep the current scene visible if loading could not begin.
                yield return FadeRoutine(0f, null);
                yield break;
            }

            while (!op.isDone) yield return null;
            yield return FadeRoutine(0f, null);
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
            float startAlpha = _fadeImage.color.a;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlpha(targetAlpha);
            onComplete?.Invoke();
        }

        private void SetAlpha(float a)
        {
            if (_fadeImage == null) return;
            var c = _fadeImage.color;
            c.a = a;
            _fadeImage.color = c;
        }
    }
}
