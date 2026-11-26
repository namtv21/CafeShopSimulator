using System.Collections.Generic;
using UnityEngine;

namespace CafeShopSimulator
{
    /// <summary>Fixed-position cards observe independent slot models; only CafeApp advances time.</summary>
    public sealed class OrderScreenController : MonoBehaviour
    {
        [SerializeField] private OrderCardView orderCardPrefab;
        [SerializeField] private OrderCardView mapOrderCardPrefab;
        [SerializeField] private Transform baristaContainer, mapContainer;
        private readonly List<OrderCardView> baristaCards = new List<OrderCardView>();
        private readonly List<OrderCardView> mapCards = new List<OrderCardView>();
        public IReadOnlyList<OrderCardView> BaristaCards => baristaCards.AsReadOnly();
        public IReadOnlyList<OrderCardView> MapCards => mapCards.AsReadOnly();
        // Previous single-customer tooling can still inspect the first card.
        public OrderCardView BaristaCard => baristaCards.Count > 0 ? baristaCards[0] : null;
        public OrderCardView MapCard => mapCards.Count > 0 ? mapCards[0] : null;
        private void Start()
        {
            var shift = CafeApp.Instance != null ? CafeApp.Instance.Shift : null;
            if (shift == null) return;
            for (int i = 0; i < shift.OrderCount; i++)
            {
                baristaCards.Add(Create(shift, i, baristaContainer, true));
                if (mapContainer != null) mapCards.Add(Create(shift, i, mapContainer, false));
            }
        }

        private OrderCardView Create(ShiftController shift, int slot, Transform parent, bool allowServing)
        {
            var prefab = !allowServing && mapOrderCardPrefab != null ? mapOrderCardPrefab : orderCardPrefab;
            var card = Instantiate(prefab, parent);
            card.name = "OrderSlot-" + slot;
            card.Bind(shift, slot, allowServing);
            return card;
        }
    }
}
