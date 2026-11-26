using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    public sealed class OrderCardView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel, recipeLabel, patienceLabel, feedbackLabel;
        [SerializeField] private Image patienceFill;
        [SerializeField] private Button serveButton;
        private ShiftController shift;
        private long displayedOrderId;
        private bool allowServing;
        private int slotIndex;
        public int SlotIndex => slotIndex;
        public Button ServeButton => serveButton;

        public void Bind(ShiftController value, int slotIndex, bool allowServing)
        {
            if (shift != null) shift.Changed -= ClearFeedback;
            shift = value;
            this.slotIndex = slotIndex;
            if (shift != null) shift.Changed += ClearFeedback;
            this.allowServing = allowServing;
            serveButton.gameObject.SetActive(allowServing);
            serveButton.onClick.RemoveListener(Serve);
            serveButton.onClick.AddListener(Serve);
            Render();
        }

        private void Update() => Render();

        private void Render()
        {
            if (shift == null)
            {
                titleLabel.text = "BÀN " + (slotIndex + 1).ToString("00");
                recipeLabel.text = "Chưa có ca chơi";
                serveButton.interactable = false;
                patienceFill.fillAmount = 0;
                patienceLabel.text = "Bắt đầu từ MainMenu";
                return;
            }
            var order = shift.GetOrder(slotIndex);
            if (order.HasCustomer && order.Id != displayedOrderId) ClearFeedback();
            displayedOrderId = order.Id;
            string table = "BÀN " + (slotIndex + 1).ToString("00");
            titleLabel.text = table;
            recipeLabel.text = order.HasCustomer ? shift.Catalog.GetRecipe(order.RecipeId).displayName : "Đang chờ khách";
            patienceLabel.text = order.IsClosed ? "Quán đã đóng cửa"
                : order.HasCustomer && !order.IsSeated ? "Đang tới bàn"
                : order.HasCustomer ? Mathf.CeilToInt(order.RemainingPatience) + "s"
                : "Khách đến: " + Mathf.CeilToInt(order.ArrivalSeconds) + "s";
            patienceFill.fillAmount = order.HasCustomer ? order.RemainingPatience / order.TotalPatience : 0;
            patienceFill.color = order.HasCustomer && order.RemainingPatience / order.TotalPatience < .3f
                ? new Color(.85f, .35f, .25f) : new Color(.87f, .67f, .38f);
            serveButton.interactable = allowServing && shift.CanInteract && order.IsSeated && shift.Cup.Count > 0;
        }

        private void ClearFeedback() => feedbackLabel.text = "";

        public void Serve()
        {
            if (shift == null || !allowServing) return;
            try
            {
                switch (shift.TryServe(slotIndex, displayedOrderId))
                {
                    case ServeResult.Served: feedbackLabel.text = "Đã phục vụ"; break;
                    case ServeResult.WrongRecipe: feedbackLabel.text = "Sai món/thứ tự. Ly và đơn được giữ."; break;
                    case ServeResult.EmptyCup: feedbackLabel.text = "Hãy pha đồ uống trước."; break;
                    case ServeResult.NoCustomer: feedbackLabel.text = "Chưa có khách để phục vụ."; break;
                    case ServeResult.StaleOrder: feedbackLabel.text = "Đơn đã thay đổi. Hãy kiểm tra lại."; break;
                    case ServeResult.InvalidSlot: feedbackLabel.text = "Vị trí không hợp lệ."; break;
                    case ServeResult.CustomerArriving: feedbackLabel.text = "Khách đang tới bàn."; break;
                    case ServeResult.InteractionBlocked: return;
                }
                Render();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                feedbackLabel.text = "Chưa thể ghi nhận phục vụ. Ly và đơn được giữ.";
            }
        }

        private void OnDestroy()
        {
            if (shift != null) shift.Changed -= ClearFeedback;
            if (serveButton != null) serveButton.onClick.RemoveListener(Serve);
        }
    }
}
