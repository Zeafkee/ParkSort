using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkSort.UI
{
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 0.92f;

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(pressedScale, 0.07f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1f, 0.16f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}
