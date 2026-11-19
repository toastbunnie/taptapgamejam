using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class EndingEffect : MonoBehaviour
{
    [Header("黑幕遮罩（从中心向四周扩散）")]
    public Image fadePanel;
    public Color maskColor = Color.black;

    [Header("结局显示内容")]
    public float fadeDuration = 5f;
    [Min(0f)] public float cgDisplayDuration = 2f;
    [Min(0f)] public float cgFadeDuration = 0.8f;
    [TextArea(3, 8)] public string creditsText = "制作人员";
    [Min(0f)] public float creditsDisplayDuration = 3f;
    public GameObject endingTitle;
    public GameObject endingName;
    public GameObject restartButton;
    public GameObject mainMenuButton;
    public UnityEngine.UI.Image endingCG;

    private Material _centerRevealMaterial;
    private Coroutine _endingCoroutine;
    private float _fullMaskRadius = 1f;
    private string _currentEndingTitle;
    private string _currentEndingName;
    private TMP_Text _endingTitleText;
    private RectTransform _endingTitleRect;
    private Vector2 _titleAnchorMin;
    private Vector2 _titleAnchorMax;
    private Vector2 _titleAnchoredPosition;
    private Vector2 _titleSizeDelta;
    private Vector2 _titlePivot;
    private float _titleFontSize;
    private float _titleFontSizeMin;
    private float _titleFontSizeMax;
    private bool _titleAutoSizing;
    private TextAlignmentOptions _titleAlignment;
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");

    private void Awake()
    {
        CacheEndingTitleLayout();
        EnsureMaskMaterial();
    }

    private void CacheEndingTitleLayout()
    {
        if (endingTitle == null)
            return;

        _endingTitleText = endingTitle.GetComponent<TMP_Text>();
        if (_endingTitleText == null)
            _endingTitleText = endingTitle.GetComponentInChildren<TMP_Text>(true);
        if (_endingTitleText == null)
            return;

        _endingTitleRect = _endingTitleText.rectTransform;
        _titleAnchorMin = _endingTitleRect.anchorMin;
        _titleAnchorMax = _endingTitleRect.anchorMax;
        _titleAnchoredPosition = _endingTitleRect.anchoredPosition;
        _titleSizeDelta = _endingTitleRect.sizeDelta;
        _titlePivot = _endingTitleRect.pivot;
        _titleFontSize = _endingTitleText.fontSize;
        _titleFontSizeMin = _endingTitleText.fontSizeMin;
        _titleFontSizeMax = _endingTitleText.fontSizeMax;
        _titleAutoSizing = _endingTitleText.enableAutoSizing;
        _titleAlignment = _endingTitleText.alignment;
    }

    private void ConfigureCreditsLayout()
    {
        if (_endingTitleText == null || _endingTitleRect == null)
            return;

        _endingTitleRect.anchorMin = new Vector2(0.04f, 0.05f);
        _endingTitleRect.anchorMax = new Vector2(0.96f, 0.95f);
        _endingTitleRect.pivot = new Vector2(0.5f, 0.5f);
        _endingTitleRect.anchoredPosition = Vector2.zero;
        _endingTitleRect.sizeDelta = Vector2.zero;

        _endingTitleText.enableAutoSizing = true;
        _endingTitleText.fontSize = 48f;
        _endingTitleText.fontSizeMin = 28f;
        _endingTitleText.fontSizeMax = 48f;
        _endingTitleText.alignment = TextAlignmentOptions.Center;
        _endingTitleText.enableWordWrapping = true;
    }

    private void RestoreEndingTitleLayout()
    {
        if (_endingTitleText == null || _endingTitleRect == null)
            return;

        _endingTitleRect.anchorMin = _titleAnchorMin;
        _endingTitleRect.anchorMax = _titleAnchorMax;
        _endingTitleRect.anchoredPosition = _titleAnchoredPosition;
        _endingTitleRect.sizeDelta = _titleSizeDelta;
        _endingTitleRect.pivot = _titlePivot;
        _endingTitleText.fontSize = _titleFontSize;
        _endingTitleText.fontSizeMin = _titleFontSizeMin;
        _endingTitleText.fontSizeMax = _titleFontSizeMax;
        _endingTitleText.enableAutoSizing = _titleAutoSizing;
        _endingTitleText.alignment = _titleAlignment;
    }

    public void SetEndingCG(Sprite cg)
    {
        if (endingCG != null)
        {
            endingCG.sprite = cg;
            endingCG.color = Color.white;
        }
    }

    public void PlayEnding(EndingData data)
    {
        if (data == null)
        {
            Debug.LogError("[EndingEffect] 没有绑定结局数据。", this);
            return;
        }

        // EndingPanel 初始是隐藏的，由外部控制器通过此方法启动演出。
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SetText(endingTitle, data.endingTitle);
        SetText(endingName, data.endingName);
        _currentEndingTitle = data.endingTitle;
        _currentEndingName = data.endingName;
        SetEndingCG(data.endingCG);
        maskColor = data.maskColor;
        EnsureMaskMaterial();
        UpdateMaskAspect();

        if (_endingCoroutine != null)
            StopCoroutine(_endingCoroutine);

        _endingCoroutine = StartCoroutine(FadeIn());
    }

    private static void SetText(GameObject textObject, string value)
    {
        if (textObject == null)
            return;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (text == null)
            text = textObject.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = value ?? "";
    }

    private IEnumerator AnimateMask(float fromRadius, float toRadius)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            float radius = Mathf.Lerp(fromRadius, toRadius, t);

            if (_centerRevealMaterial != null)
                _centerRevealMaterial.SetFloat(RadiusId, radius);

            yield return null;
        }

        if (_centerRevealMaterial != null)
            _centerRevealMaterial.SetFloat(RadiusId, toRadius);
    }

    public IEnumerator FadeIn()
    {
        if (fadePanel == null)
        {
            Debug.LogWarning("[EndingEffect] 未绑定 fadePanel，跳过黑幕扩散效果。");
            yield break;
        }

        EnsureMaskMaterial();
        if (_centerRevealMaterial == null)
        {
            Debug.LogError("[EndingEffect] 找不到 UI/CenterReveal Shader，无法播放结局遮罩。", this);
            yield break;
        }
        UpdateMaskAspect();

        fadePanel.raycastTarget = true;
        fadePanel.enabled = true;
        if (endingCG != null)
        {
            endingCG.gameObject.SetActive(true);
            endingCG.color = new Color(1f, 1f, 1f, 0f);
        }

        SetActiveIfAssigned(endingTitle, false);
        SetActiveIfAssigned(endingName, false);
        SetActiveIfAssigned(restartButton, false);
        SetActiveIfAssigned(mainMenuButton, false);

        // 从原画面开始，让黑色圆形向外扩散并盖住整个画面。
        _centerRevealMaterial.SetFloat(RadiusId, 0f);
        yield return AnimateMask(0f, _fullMaskRadius);

        // 黑场保持时淡入 CG，再从中心向外退去，让 CG 显现。
        if (endingCG != null)
            yield return FadeCG(0f, 1f, cgFadeDuration);
        yield return AnimateMask(_fullMaskRadius, 0f);

        if (cgDisplayDuration > 0f)
            yield return new WaitForSeconds(cgDisplayDuration);

        // 黑色再次从中心扩散，盖住 CG 后显示结局信息。
        yield return AnimateMask(0f, _fullMaskRadius);

        // CG 结束后先在全黑画面显示制作人员，再显示结局信息。
        ConfigureCreditsLayout();
        SetText(endingTitle, creditsText);
        SetActiveIfAssigned(endingTitle, true);
        SetActiveIfAssigned(endingName, false);
        if (creditsDisplayDuration > 0f)
            yield return new WaitForSecondsRealtime(creditsDisplayDuration);

        RestoreEndingTitleLayout();
        SetText(endingTitle, _currentEndingTitle);
        SetText(endingName, _currentEndingName);
        SetActiveIfAssigned(endingTitle, true);
        SetActiveIfAssigned(endingName, true);
        SetActiveIfAssigned(restartButton, true);
        SetActiveIfAssigned(mainMenuButton, true);
        _endingCoroutine = null;
    }

    private void EnsureMaskMaterial()
    {
        if (fadePanel == null || _centerRevealMaterial != null)
            return;

        Shader shader = Shader.Find("UI/CenterReveal");
        if (shader == null)
            return;

        _centerRevealMaterial = new Material(shader);
        fadePanel.material = _centerRevealMaterial;
        fadePanel.color = Color.white;
        _centerRevealMaterial.SetVector(CenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
        _centerRevealMaterial.SetFloat(SoftnessId, 0.08f);
        _centerRevealMaterial.SetFloat(RadiusId, 0f);
        _centerRevealMaterial.SetColor(MaskColorId, maskColor);
        UpdateMaskAspect();
    }

    private void UpdateMaskAspect()
    {
        if (_centerRevealMaterial == null || fadePanel == null)
            return;

        Rect rect = fadePanel.rectTransform.rect;
        float aspect = rect.height > 0f ? rect.width / rect.height : 1f;
        _fullMaskRadius = Mathf.Sqrt(0.25f * aspect * aspect + 0.25f) + 0.08f;
        _centerRevealMaterial.SetFloat(AspectId, aspect);
        _centerRevealMaterial.SetColor(MaskColorId, maskColor);
    }

    private IEnumerator FadeCG(float fromAlpha, float toAlpha, float duration)
    {
        if (endingCG == null)
            yield break;

        float time = 0f;
        Color color = endingCG.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(time / duration));
            endingCG.color = color;
            yield return null;
        }

        color.a = toAlpha;
        endingCG.color = color;
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
