using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class DialogueHistoryPage : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private ScrollRect _scrollRect;
        private RectTransform _contentRect;
        private TMP_Text _historyText;
        private Action _onBack;
        public bool IsVisible { get; private set; }

        public void Initialize(Transform parent, TMP_FontAsset font, Button backStyleSource, Action onBack)
        {
            transform.SetParent(parent, false);
            transform.SetAsLastSibling();

            RectTransform rootRect = GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _onBack = onBack;

            Image background = GetComponent<Image>();
            background.color = new Color(0.105f, 0.114f, 0.165f, 0.98f);
            background.raycastTarget = true;

            // A Button gives the full-screen background a UI pointer handler, so
            // clicks on empty parts of the page are consumed by this panel.
            Button inputBlocker = GetComponent<Button>();
            if (inputBlocker != null)
            {
                inputBlocker.targetGraphic = background;
                inputBlocker.transition = Selectable.Transition.None;
                inputBlocker.interactable = true;
            }

            BuildHeader(font, backStyleSource);
            BuildHistoryScroll(font);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshHistory();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            IsVisible = true;
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            gameObject.SetActive(false);
        }

        private void BuildHeader(TMP_FontAsset font, Button backStyleSource)
        {
            TMP_Text title = CreateText("HistoryTitle", transform, font, 42f,
                new Color(0.94f, 0.91f, 0.85f, 1f));
            SetRect(title.rectTransform, new Vector2(0.08f, 0.87f), new Vector2(0.92f, 0.98f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            title.text = "历史回顾";
            title.alignment = TextAlignmentOptions.Center;

            if (backStyleSource != null)
            {
                GameObject clonedButtonObject = Instantiate(backStyleSource.gameObject, transform, false);
                clonedButtonObject.name = "HistoryBackButton";

                Button clonedButton = clonedButtonObject.GetComponent<Button>();
                if (clonedButton != null)
                {
                    clonedButton.onClick.RemoveAllListeners();
                    clonedButton.onClick.AddListener(BackToSettings);
                }

                TMP_Text clonedButtonLabel = clonedButtonObject.GetComponentInChildren<TMP_Text>(true);
                if (clonedButtonLabel != null)
                    clonedButtonLabel.text = "返回";
                return;
            }

            GameObject fallbackButtonObject = CreateUIObject("BackButton", transform, typeof(Image), typeof(Button));
            RectTransform buttonRect = fallbackButtonObject.GetComponent<RectTransform>();
            SetRect(buttonRect, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-55f, -45f), new Vector2(220f, 72f));

            Image buttonImage = fallbackButtonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.82f, 0.76f, 0.68f, 1f);
            Button button = fallbackButtonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(BackToSettings);

            TMP_Text buttonLabel = CreateText("Label", fallbackButtonObject.transform, font, 24f,
                new Color(0.19f, 0.2f, 0.29f, 1f));
            SetRect(buttonLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            buttonLabel.text = "返回";
            buttonLabel.alignment = TextAlignmentOptions.Center;
        }

        private void BuildHistoryScroll(TMP_FontAsset font)
        {
            GameObject scrollObject = CreateUIObject("HistoryScroll", transform, typeof(ScrollRect));
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetRect(scrollRectTransform, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.85f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject viewportObject = CreateUIObject("Viewport", scrollObject.transform, typeof(Image), typeof(Mask));
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.15f, 0.16f, 0.22f, 0.86f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = true;

            GameObject contentObject = CreateUIObject("Content", viewportObject.transform);
            _contentRect = contentObject.GetComponent<RectTransform>();
            SetRect(_contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

            _historyText = CreateText("HistoryText", contentObject.transform, font, 32f,
                new Color(0.94f, 0.91f, 0.85f, 1f));
            RectTransform textRect = _historyText.rectTransform;
            SetRect(textRect, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-48f, 0f));
            _historyText.alignment = TextAlignmentOptions.TopLeft;
            _historyText.enableWordWrapping = true;
            _historyText.richText = false;
            _historyText.margin = new Vector4(12f, 8f, 12f, 8f);

            _scrollRect = scrollObject.GetComponent<ScrollRect>();
            _scrollRect.viewport = viewportRect;
            _scrollRect.content = _contentRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 35f;
        }

        private void RefreshHistory()
        {
            var entries = SaveManager.Instance != null
                ? SaveManager.Instance.GetDialogueHistory()
                : new System.Collections.Generic.List<SaveManager.DialogueHistoryEntry>();

            StringBuilder builder = new StringBuilder();
            string previousScene = null;

            foreach (SaveManager.DialogueHistoryEntry entry in entries)
            {
                if (entry == null)
                    continue;

                if (!string.Equals(previousScene, entry.sceneName, StringComparison.Ordinal))
                {
                    if (builder.Length > 0)
                        builder.AppendLine();
                    builder.Append("【").Append(entry.sceneName).AppendLine("】");
                    previousScene = entry.sceneName;
                }

                if (!string.IsNullOrWhiteSpace(entry.speaker))
                    builder.Append(entry.speaker).AppendLine("：");
                builder.AppendLine(entry.content);
                builder.AppendLine();
            }

            _historyText.text = builder.Length > 0 ? builder.ToString() : "暂无历史对话";
            Canvas.ForceUpdateCanvases();
            float textHeight = _historyText.GetPreferredValues(
                _historyText.text, _historyText.rectTransform.rect.width, 0f).y + 40f;
            float contentHeight = Mathf.Max(textHeight, _scrollRect.viewport.rect.height);
            _historyText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, Mathf.Max(0f, contentHeight - 20f));
            _contentRect.sizeDelta = new Vector2(0f, contentHeight);
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void BackToSettings()
        {
            Hide();
            _onBack?.Invoke();
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float fontSize, Color color)
        {
            GameObject textObject = CreateUIObject(name, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject created = new GameObject(name, AddRectTransform(components));
            created.transform.SetParent(parent, false);
            return created;
        }

        private static System.Type[] AddRectTransform(System.Type[] components)
        {
            System.Type[] allComponents = new System.Type[components.Length + 1];
            allComponents[0] = typeof(RectTransform);
            Array.Copy(components, 0, allComponents, 1, components.Length);
            return allComponents;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
