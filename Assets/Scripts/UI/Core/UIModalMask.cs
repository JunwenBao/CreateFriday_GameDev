using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>所有模态窗口共用的一张遮罩，始终位于最上层模态窗口正下方，避免多层叠加越来越暗。</summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup))]
    public class UIModalMask : MonoBehaviour, IPointerClickHandler
    {
        private Canvas _canvas;
        private CanvasGroup _group;
        private float _fadeDuration;
        private float _targetAlpha;

        public event Action Clicked;

        internal static UIModalMask Create(Transform parent, Color color, float fadeDuration)
        {
            var rect = UIManager.CreateStretchedRect("ModalMask", parent);
            var mask = rect.gameObject.AddComponent<UIModalMask>();
            rect.gameObject.AddComponent<Image>().color = color;

            mask._canvas = mask.GetComponent<Canvas>();
            mask._group = mask.GetComponent<CanvasGroup>();
            mask._group.alpha = 0f;
            mask._fadeDuration = fadeDuration;
            rect.gameObject.SetActive(false);
            return mask;
        }

        /// <summary>移到 sortingOrder 处；visible 为 false 时随关闭中的窗口一起淡出。</summary>
        internal void Place(int sortingOrder, bool visible)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
            _targetAlpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }

        internal void Hide()
        {
            _targetAlpha = 0f;
            _group.blocksRaycasts = false;
        }

        internal void Tick(float deltaTime)
        {
            if (!gameObject.activeSelf)
                return;

            _group.alpha = _fadeDuration > 0f
                ? Mathf.MoveTowards(_group.alpha, _targetAlpha, deltaTime / _fadeDuration)
                : _targetAlpha;

            if (_group.alpha <= 0f && _targetAlpha <= 0f)
                gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
    }
}
