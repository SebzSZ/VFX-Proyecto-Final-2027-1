using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinalVFX.Menu
{
    [RequireComponent(typeof(ScrollRect))]
    [DefaultExecutionOrder(100)]
    public sealed class DropdownSelectionScroll : MonoBehaviour
    {
        private ScrollRect _scroll;
        private GameObject _lastSelected;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake() => _scroll = GetComponent<ScrollRect>();

        private void OnEnable() => _lastSelected = null;

        private void LateUpdate()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;

            if (selected == null || _scroll.content == null ||
                !selected.transform.IsChildOf(_scroll.content))
            {
                _lastSelected = null;
                return;
            }

            if (selected == _lastSelected ||
                !(selected.transform is RectTransform item))
                return;

            _lastSelected = selected;
            Canvas.ForceUpdateCanvases();

            RectTransform viewport = _scroll.viewport != null
                ? _scroll.viewport : (RectTransform)_scroll.transform;
            Bounds itemBounds = BoundsInViewport(item, viewport);
            Bounds contentBounds = BoundsInViewport(_scroll.content, viewport);
            Rect view = viewport.rect;
            float hiddenHeight = contentBounds.size.y - view.height;
            if (hiddenHeight <= 0f)
                return;

            float offset = 0f;
            if (itemBounds.max.y > view.yMax)
                offset = view.yMax - itemBounds.max.y;
            else if (itemBounds.min.y < view.yMin)
                offset = view.yMin - itemBounds.min.y;

            if (Mathf.Abs(offset) < 0.01f)
                return;

            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(
                _scroll.verticalNormalizedPosition - offset / hiddenHeight);
        }

        private Bounds BoundsInViewport(RectTransform rect, RectTransform viewport)
        {
            rect.GetWorldCorners(_corners);
            var bounds = new Bounds(viewport.InverseTransformPoint(_corners[0]), Vector3.zero);
            for (int i = 1; i < _corners.Length; i++)
                bounds.Encapsulate(viewport.InverseTransformPoint(_corners[i]));
            return bounds;
        }
    }
}
