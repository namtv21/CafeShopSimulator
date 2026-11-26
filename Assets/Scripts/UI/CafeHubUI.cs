using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    /// <summary>Authored panel bindings. Content creates prefab rows, never scene layout.</summary>
    public sealed class CafeHubUI : MonoBehaviour
    {
        [Serializable] public struct IngredientIcon { public string id; public Sprite sprite; }
        public GameObject panel;
        public Text heading, detail, status;
        public Transform rows;
        public ScrollRect scroll;
        public HubItemView rowPrefab;
        public HubItemView shopPrefab;
        public RectTransform shopRows;
        private Transform listRows;
        public IngredientIcon[] ingredientIcons;
        private string section;
        private bool wasPaused;
        private CafeApp App => CafeApp.Instance;
        public void OpenShop() => Open("shop");
        public void OpenUpgrades() => Open("upgrades");
        public void OpenChat() => Open("chat");
        public void OpenBackroom() => Open("backroom");
        public void OpenClients() => Open("clients");
        public void Quit() => Application.Quit();
        private void Open(string target)
        {
            if (App == null || App.Days == null) return;
            if (!panel.activeSelf) { wasPaused = App.IsPaused; App.SetPaused(true); }
            section = target; panel.SetActive(true); status.text = ""; Render();
        }
        public void Close()
        {
            if (!panel.activeSelf) return;
            panel.SetActive(false); App.SetPaused(wasPaused);
        }
        private void Transaction(string action, string id)
        {
            bool ok = App.TryHubAction(action, id, out string message);
            Render();
            status.text = ok && action == "talk"
                ? App.Hub.dialogue[(App.Days.Snapshot.affection - 1) % App.Hub.dialogue.Length] : message;
        }
        private void Row(string name, string description, string label = "", bool enabled = false,
            Action action = null, Sprite icon = null)
        {
            var row = Instantiate(section == "shop" ? shopPrefab : rowPrefab, rows);
            row.Bind(name, description, label, enabled, icon, action);
        }
        private void Render()
        {
            // Detach before deferred Destroy so LayoutGroup never counts stale rows.
            for (int i = rows.childCount - 1; i >= 0; i--)
            { var child = rows.GetChild(i); child.gameObject.SetActive(false); child.SetParent(null); Destroy(child.gameObject); }
            if (listRows == null) listRows = rows;
            rows = section == "shop" ? shopRows : listRows;
            listRows.gameObject.SetActive(section != "shop");
            shopRows.gameObject.SetActive(section == "shop");
            scroll.content = (RectTransform)rows;
            var p = App.Days.Snapshot;
            bool editable = p.phase != DayPhase.Playing;
            switch (section)
            {
                case "shop":
                    heading.text = "Nguyên liệu";
                    detail.text = $"Ngày {HubRules.StockDay(p)} · Một gói / một ca · {p.wallet:N0} xu";
                    foreach (var id in App.Content.IngredientIds)
                    {
                        var item = App.Content.GetIngredient(id);
                        bool owned = HubRules.HasPack(p, item);
                        Sprite sprite = null;
                        foreach (var entry in ingredientIcons ?? new IngredientIcon[0]) if (entry.id == id) { sprite = entry.sprite; break; }
                        Row(item.displayName, item.alwaysAvailable ? "Luôn có" : "Dùng không giới hạn trong ca",
                            owned ? "Đã có" : item.packPrice + " xu", editable && !owned && p.wallet >= item.packPrice,
                            () => Transaction("pack", id), sprite);
                    }
                    break;
                case "upgrades":
                    heading.text = "Nâng cấp & nhân viên"; detail.text = $"{p.wallet:N0} xu · Hiệu lực từ ca kế tiếp";
                    foreach (var item in App.Hub.upgrades)
                    {
                        bool owned = HubRules.Owns(p.upgrades, item.id);
                        Row(item.name, item.description, owned ? "Đã có" : item.price + " xu",
                            editable && !owned && p.wallet >= item.price
                                && (string.IsNullOrEmpty(item.requiresUpgradeId) || HubRules.Owns(p.upgrades, item.requiresUpgradeId)),
                            () => Transaction("upgrade", item.id));
                    }
                    foreach (var employee in App.Hub.staff ?? new StaffDefinition[0])
                    {
                        bool hired = p.staffId == employee.id;
                        Row(employee.name, employee.description,
                            hired ? "Đã thuê" : "Thuê · " + employee.hirePrice + " xu",
                            editable && string.IsNullOrEmpty(p.staffId) && p.wallet >= employee.hirePrice,
                            () => Transaction("hire", employee.id));
                    }
                    break;
                case "chat":
                    heading.text = App.Hub.hostName; detail.text = $"Thân thiết {p.affection} · 1 cuộc trò chuyện / ngày";
                    Row("Hỏi thăm", "Một chút thời gian sau giờ làm.", p.lastChatDay == p.dayNumber ? "Ngày mai" : "Trò chuyện",
                        editable && p.lastChatDay != p.dayNumber, () => Transaction("talk", ""));
                    Row("Lời nhờ nhỏ", $"Phục vụ {App.Hub.questServed} khách · Thưởng {App.Hub.questReward} xu",
                        p.lastQuestDay == p.dayNumber ? "Đã nhận" : $"{p.servedCustomers}/{App.Hub.questServed}",
                        p.phase == DayPhase.Summary && p.servedCustomers >= App.Hub.questServed && p.lastQuestDay != p.dayNumber,
                        () => Transaction("quest", ""));
                    break;
                case "backroom":
                    heading.text = "Phòng nghỉ"; detail.text = App.HasBackroom ? "Ngoài giờ làm" : "Mở khóa trong Nâng cấp";
                    if (App.HasBackroom)
                        Row(App.Hub.hostName, "Đang nghỉ tại phòng nhân viên.", "Trò chuyện", editable, OpenChat);
                    else Row("Cửa đang khóa", "Mua nâng cấp Phòng nghỉ.", "Nâng cấp", editable, OpenUpgrades);
                    break;
                case "clients":
                    heading.text = "Khách hàng"; detail.text = $"Tổng đã phục vụ: {p.totalServed}";
                    Row("Ca hiện tại", $"Phục vụ {p.servedCustomers} · Rời quán {p.missedCustomers}");
                    foreach (var id in App.Content.RecipeIds)
                    {
                        var recipe = App.Content.GetRecipe(id);
                        Row(recipe.displayName, recipe.unlockDay <= p.dayNumber ? "Có trong thực đơn" : "Mở ngày " + recipe.unlockDay,
                            recipe.sellPrice + " xu");
                    }
                    break;
            }
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        }
    }
}
