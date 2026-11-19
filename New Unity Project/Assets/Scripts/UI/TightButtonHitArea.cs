using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Keeps a button's visual size while restricting pointer hits to the
    /// normalized rectangle occupied by its visible sprite artwork.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class TightButtonHitArea : MonoBehaviour
    {
        [SerializeField] private Vector2 _normalizedMin = Vector2.zero;
        [SerializeField] private Vector2 _normalizedMax = Vector2.one;

        private const string HitAreaName = "TightHitArea";

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Vector2 min = Vector2.Min(_normalizedMin, _normalizedMax);
            Vector2 max = Vector2.Max(_normalizedMin, _normalizedMax);
            _normalizedMin = min;
            _normalizedMax = max;
        }

        private void Apply()
        {
            if (GetComponent<Button>() == null)
                return;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
                graphic.raycastTarget = false;

            Transform existingHitArea = transform.Find(HitAreaName);
            GameObject hitAreaObject = existingHitArea != null
                ? existingHitArea.gameObject
                : new GameObject(HitAreaName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hitAreaObject.transform.SetParent(transform, false);

            RectTransform hitRect = hitAreaObject.GetComponent<RectTransform>();
            hitRect.anchorMin = _normalizedMin;
            hitRect.anchorMax = _normalizedMax;
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            hitRect.localScale = Vector3.one;

            Image hitGraphic = hitAreaObject.GetComponent<Image>();
            hitGraphic.color = new Color(1f, 1f, 1f, 0f);
            hitGraphic.raycastTarget = true;

            // Let pointer events bubble from the hit graphic to the Button.
            hitAreaObject.transform.SetAsLastSibling();
        }
    }
}
