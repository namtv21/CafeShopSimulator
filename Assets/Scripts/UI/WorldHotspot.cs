using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CafeShopSimulator
{
    /// <summary>Clickable sprite; uses the EventSystem, not legacy mouse input.</summary>
    public sealed class WorldHotspot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public SpriteRenderer[] visuals;
        public UnityEvent clicked = new UnityEvent();
        private void Tint(Color tint) { foreach (var sprite in visuals) if(sprite != null) sprite.color=tint; }
        public void OnPointerEnter(PointerEventData data) => Tint(new Color(1.3f,1.3f,1.1f));
        public void OnPointerExit(PointerEventData data) => Tint(Color.white);
        public void OnPointerClick(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left)clicked.Invoke(); }
        private void OnDisable() => Tint(Color.white);
    }
}
