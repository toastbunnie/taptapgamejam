using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Game.Core.Emotion
{
    /// <summary>
    /// 情绪条：单条数值 + 由触发点指定的固定颜色，无数值标注。
    ///
    /// 用法：
    ///   - 挂到场景 UI 节点，并引用填充容器 Image
    ///   - 调用 EmotionBar.Instance.AddEmotion(amount) 增/减情绪
    ///   - 越过暖阈值 / 热阈值时分别触发事件
    /// </summary>
    public class EmotionBar : MonoBehaviour
    {
        public static EmotionBar Instance { get; private set; }

        [Header("UI")]
        [Tooltip("填充容器 Image；运行时会在内部按颜色创建分段")]
        [SerializeField] private Image _fillImage;

        [Header("数值")]
        [Tooltip("情绪条最大数值")]
        [SerializeField] private float _maxValue = 100f;

        [Header("阈值（maxValue 比例 0~1）")]
        [Range(0, 1)] [SerializeField] private float _warmThreshold = 0.4f;
        [Range(0, 1)] [SerializeField] private float _hotThreshold = 0.8f;

        [Header("事件")]
        [Tooltip("越过暖阈值时触发一次")]
        public UnityEvent OnWarmThresholdReached;
        [Tooltip("越过热阈值时触发一次（情绪最高）")]
        public UnityEvent OnHotThresholdReached;

        private struct EmotionSegmentData
        {
            public float value;
            public Color color;

            public EmotionSegmentData(float value, Color color)
            {
                this.value = value;
                this.color = color;
            }
        }

        private static float s_currentValue;
        private static bool s_warmTriggeredThisCycle;
        private static bool s_hotTriggeredThisCycle;
        private static Color s_fillColor = Color.white;
        private static readonly List<EmotionSegmentData> s_segments =
            new List<EmotionSegmentData>();

        private Sprite _generatedFillSprite;
        private bool _ownsGeneratedFillSprite;
        private readonly List<Image> _segmentViews = new List<Image>();

        public float CurrentValue => s_currentValue;
        public float MaxValue => _maxValue;
        public float Progress => _maxValue > 0f ? s_currentValue / _maxValue : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            s_currentValue = 0f;
            s_warmTriggeredThisCycle = false;
            s_hotTriggeredThisCycle = false;
            s_fillColor = Color.white;
            s_segments.Clear();
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;

            // 使用纯色方形 Sprite，确保 Filled Image 的内部没有默认圆角或纹理空隙。
            if (_fillImage != null)
            {
                if (_fillImage.sprite == null)
                {
                    _generatedFillSprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, 1f, 1f),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    _ownsGeneratedFillSprite = true;
                    _fillImage.sprite = _generatedFillSprite;
                }
                else
                    _generatedFillSprite = _fillImage.sprite;

                _fillImage.enabled = false;
            }

            RebuildSegmentViews();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_ownsGeneratedFillSprite && _generatedFillSprite != null)
                Destroy(_generatedFillSprite);
        }

        private void Update()
        {
            UpdateVisuals();
        }

        /// <summary>增加 / 减少情绪（amount 可为负）</summary>
        public void AddEmotion(float amount)
        {
            AddEmotion(amount, s_fillColor);
        }

        /// <summary>累加情绪值，并使用本次触发指定的填充颜色。</summary>
        public void AddEmotion(float amount, Color fillColor)
        {
            float previousValue = s_currentValue;
            s_currentValue = Mathf.Clamp(s_currentValue + amount, 0f, _maxValue);

            float appliedAmount = s_currentValue - previousValue;
            if (appliedAmount > 0f)
            {
                s_fillColor = fillColor;
                s_segments.Add(new EmotionSegmentData(appliedAmount, fillColor));
            }
            else if (appliedAmount < 0f)
                RemoveFromEnd(-appliedAmount);

            RebuildSegmentViews();
            CheckThresholds();
        }

        /// <summary>直接设置填充长度和颜色，供需要定制显示的情绪触发点使用。</summary>
        public void SetEmotion(float value, Color fillColor)
        {
            ClearSegmentData();
            s_currentValue = Mathf.Clamp(value, 0f, _maxValue);
            s_fillColor = fillColor;
            if (s_currentValue > 0f)
                s_segments.Add(new EmotionSegmentData(s_currentValue, fillColor));
            RebuildSegmentViews();
            CheckThresholds();
        }

        private void CheckThresholds()
        {
            float p = Progress;
            if (!s_warmTriggeredThisCycle && p >= _warmThreshold)
            {
                s_warmTriggeredThisCycle = true;
                OnWarmThresholdReached?.Invoke();
            }
            if (!s_hotTriggeredThisCycle && p >= _hotThreshold)
            {
                s_hotTriggeredThisCycle = true;
                OnHotThresholdReached?.Invoke();
            }
        }

        /// <summary>重置情绪条（新一轮）</summary>
        public void ResetEmotion()
        {
            ResetForNewGame();
        }

        /// <summary>重置静态情绪数据；主菜单开始新游戏时也可在未加载情绪条场景时调用。</summary>
        public static void ResetForNewGame()
        {
            s_currentValue = 0f;
            s_warmTriggeredThisCycle = false;
            s_hotTriggeredThisCycle = false;
            s_fillColor = Color.white;
            s_segments.Clear();

            if (Instance != null)
            {
                Instance.RebuildSegmentViews();
                Instance.UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (_fillImage == null) return;

            RectTransform container = _fillImage.rectTransform;
            float containerWidth = container.rect.width;
            float offset = 0f;

            int viewCount = Mathf.Min(_segmentViews.Count, s_segments.Count);
            for (int i = 0; i < viewCount; i++)
            {
                Image view = _segmentViews[i];
                if (view == null)
                    continue;

                EmotionSegmentData segment = s_segments[i];

                float segmentWidth = _maxValue > 0f
                    ? containerWidth * segment.value / _maxValue
                    : 0f;

                RectTransform rect = view.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(offset, 0f);
                rect.sizeDelta = new Vector2(segmentWidth, 0f);
                view.color = segment.color;
                offset += segmentWidth;
            }
        }

        private void RebuildSegmentViews()
        {
            if (_fillImage == null || _generatedFillSprite == null)
                return;

            ClearSegmentViews();
            foreach (EmotionSegmentData segment in s_segments)
            {
                GameObject segmentObject = new GameObject(
                    "EmotionSegment",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );
                segmentObject.transform.SetParent(_fillImage.transform, false);

                Image image = segmentObject.GetComponent<Image>();
                image.sprite = _generatedFillSprite;
                image.type = Image.Type.Simple;
                image.raycastTarget = false;
                image.color = segment.color;
                _segmentViews.Add(image);
            }

            UpdateVisuals();
        }

        private void RemoveFromEnd(float amount)
        {
            for (int i = s_segments.Count - 1; i >= 0 && amount > 0f; i--)
            {
                EmotionSegmentData segment = s_segments[i];
                float removed = Mathf.Min(segment.value, amount);
                segment.value -= removed;
                amount -= removed;

                if (segment.value <= 0.0001f)
                    s_segments.RemoveAt(i);
                else
                    s_segments[i] = segment;
            }
        }

        private void ClearSegmentData()
        {
            s_segments.Clear();
            ClearSegmentViews();
        }

        private void ClearSegmentViews()
        {
            foreach (Image view in _segmentViews)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
            _segmentViews.Clear();
        }
    }
}
