using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkSort.UI
{
    public sealed class TapArea : MonoBehaviour, IPointerDownHandler
    {
        public event Action<Vector2> Tapped;

        public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke(eventData.position);
    }
}
