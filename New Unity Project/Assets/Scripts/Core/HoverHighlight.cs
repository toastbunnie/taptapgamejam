using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Core.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class HoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("描边设置")]
        [SerializeField] private Color _outlineColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float _outlineScale = 1.08f;

        [Header("悬停放大")]
        [SerializeField] private float _hoverScale = 1.08f;

        private Vector3 _originalScale;

        private SpriteRenderer _renderer;
        private GameObject _outlineObject;
        private SpriteRenderer _outlineRenderer;
        private bool _isHovered;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _originalScale = transform.localScale;

            if (_renderer == null)
                return;

            CreateOutline();
        }

        private void CreateOutline()
        {
            _outlineObject = new GameObject("HoverOutline");

            _outlineObject.transform.SetParent(transform);
            _outlineObject.transform.localPosition = Vector3.zero;
            _outlineObject.transform.localRotation = Quaternion.identity;
            _outlineObject.transform.localScale = Vector3.one * _outlineScale;

            _outlineRenderer = _outlineObject.AddComponent<SpriteRenderer>();

            _outlineRenderer.sprite = _renderer.sprite;
            _outlineRenderer.color = _outlineColor;

            // 放在原物体后面
            _outlineRenderer.sortingLayerID = _renderer.sortingLayerID;
            _outlineRenderer.sortingOrder = _renderer.sortingOrder - 1;

            _outlineObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;

            transform.localScale = _originalScale * _hoverScale;

            if (_outlineObject != null)
                _outlineObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;

            transform.localScale = _originalScale;

            if (_outlineObject != null)
                _outlineObject.SetActive(false);
        }

        private void OnDisable()
        {
            _isHovered = false;
            transform.localScale = _originalScale;

            if (_outlineObject != null)
                _outlineObject.SetActive(false);
        }
    }
}