using System;
using System.Collections.Generic;

namespace CafeShopSimulator
{
    [Serializable] public sealed class HubContent
    {
        public UpgradeDefinition[] upgrades;
        public string hostName;
        public string[] dialogue;
        public int questServed = 3;
        public int questReward = 30;
        public int startingTables = 1;
        public StaffDefinition[] staff = new StaffDefinition[0];
        public void Validate()
        {
            if (upgrades == null || dialogue == null || dialogue.Length == 0 || string.IsNullOrWhiteSpace(hostName)
                || questServed < 1 || questReward < 0 || startingTables < 1) throw new ArgumentException("Invalid hub content.");
            var ids = new HashSet<string>();
            foreach (var u in upgrades)
                if (u == null || string.IsNullOrWhiteSpace(u.id) || string.IsNullOrWhiteSpace(u.name)
                    || !ids.Add(u.id) || u.price < 0 || u.tipBonus < 0 || u.extraSeconds < 0 || u.extraTables < 0)
                    throw new ArgumentException("Invalid upgrade definition.");
            foreach (var u in upgrades)
                if (!string.IsNullOrEmpty(u.requiresUpgradeId) && (!ids.Contains(u.requiresUpgradeId) || u.requiresUpgradeId == u.id))
                    throw new ArgumentException("Invalid upgrade prerequisite.");
            ids.Clear();
            foreach (var employee in staff ?? new StaffDefinition[0])
            {
                if (employee == null) throw new ArgumentException("Invalid employee.");
                employee.Validate();
                if (!ids.Add(employee.id)) throw new ArgumentException("Duplicate employee ID.");
            }
        }
    }
    [Serializable] public sealed class UpgradeDefinition
    {
        public string id, name, description;
        public int price, tipBonus, extraSeconds;
        public bool opensBackroom;
        public int extraTables;
        public string requiresUpgradeId;
    }

    /// <summary>Pure transactions. Returns a candidate; caller saves before committing.</summary>
    public static class HubRules
    {
        public static bool Owns(string[] ids, string id) => Array.IndexOf(ids ?? new string[0], id) >= 0;
        public static int StockDay(CafeProgress p) => p.phase == DayPhase.Summary ? checked(p.dayNumber + 1) : p.dayNumber;
        public static bool HasPack(CafeProgress p, IngredientDefinition item) => item.alwaysAvailable
            || (StockDay(p) == 1 && item.freeOnFirstDay) || Owns(p.pendingPacks, item.id);
        private static CafeProgress Editable(CafeProgress p)
        {
            p.Validate();
            if (p.phase == DayPhase.Playing) throw new InvalidOperationException("Chỉ mua ngoài ca.");
            return p.Copy();
        }
        private static string[] Add(string[] ids, string id)
        {
            var list = new List<string>(ids ?? new string[0]); list.Add(id); return list.ToArray();
        }
        private static void Pay(CafeProgress p, int price)
        {
            if (price < 0) throw new ArgumentException("Invalid price.");
            if (p.wallet < price) throw new InvalidOperationException("Chưa đủ xu.");
            p.wallet -= price;
        }
        public static CafeProgress BuyPack(CafeProgress p, ContentCatalog catalog, string id)
        {
            var next = Editable(p); var item = catalog.GetIngredient(id);
            if (HasPack(p, item)) throw new InvalidOperationException("Đã có gói này.");
            Pay(next, item.packPrice); next.pendingPacks = Add(next.pendingPacks, id); return next;
        }
        public static CafeProgress BuyUpgrade(CafeProgress p, UpgradeDefinition item)
        {
            var next = Editable(p);
            if (Owns(p.upgrades, item.id)) throw new InvalidOperationException("Đã nâng cấp.");
            if (!string.IsNullOrEmpty(item.requiresUpgradeId) && !Owns(p.upgrades, item.requiresUpgradeId))
                throw new InvalidOperationException("Cần mở bàn trước đó.");
            Pay(next, item.price); next.upgrades = Add(next.upgrades, item.id); return next;
        }
        public static CafeProgress Hire(CafeProgress p, StaffDefinition employee)
        {
            var next = Editable(p); employee.Validate();
            if (!string.IsNullOrEmpty(p.staffId)) throw new InvalidOperationException("Quán đã có trợ lý.");
            Pay(next, employee.hirePrice); next.staffId = employee.id; return next;
        }
        public static int TableCount(CafeProgress p, HubContent hub, int maximum)
        {
            int count = hub.startingTables;
            foreach (var upgrade in hub.upgrades) if (Owns(p.upgrades, upgrade.id)) count = checked(count + upgrade.extraTables);
            return Math.Min(maximum, count);
        }
        public static CafeProgress Talk(CafeProgress p)
        {
            var next = Editable(p);
            if (p.lastChatDay == p.dayNumber) throw new InvalidOperationException("Hẹn ngày mai nhé.");
            next.affection = checked(next.affection + 1); next.lastChatDay = p.dayNumber; return next;
        }
        public static CafeProgress Claim(CafeProgress p, HubContent hub)
        {
            var next = Editable(p);
            if (p.phase != DayPhase.Summary || p.servedCustomers < hub.questServed || p.lastQuestDay == p.dayNumber)
                throw new InvalidOperationException("Chưa có thưởng để nhận.");
            next.wallet = checked(next.wallet + hub.questReward); next.lastQuestDay = p.dayNumber; return next;
        }
    }
}
