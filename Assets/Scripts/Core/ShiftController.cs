using System;
using System.Collections.Generic;

namespace CafeShopSimulator
{
    public enum ServeResult { Served, InteractionBlocked, NoCustomer, StaleOrder, EmptyCup, WrongRecipe, InvalidSlot, CustomerArriving }
    /// <summary>One shift's brewing session. Owned by CafeApp, independent of screen lifetimes.</summary>
    public sealed class ShiftController
    {
        private readonly BrewingModel brewing;
        private readonly Func<bool> canInteract;
        private readonly HashSet<string> supplies;
        private readonly OrderModel[] orders;
        private readonly Action<int> recordServed;
        private readonly Action recordMissed;
        // Slot-zero conveniences retained for the previous incremental examples/tests.
        public OrderSnapshot Order => GetOrder(0);
        public int OrderCount => orders.Length;
        public OrderSnapshot GetOrder(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= orders.Length) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            return orders[slotIndex].Snapshot;
        }
        public ContentCatalog Catalog { get; }
        public int DayNumber { get; }
        public IReadOnlyList<string> Cup => brewing.Cup;
        public IReadOnlyList<string> AvailableRecipeIds { get; }
        public bool CanInteract => canInteract();
        public StaffController Staff { get; }
        public event Action Changed;

        public ShiftController(ContentCatalog catalog, int dayNumber,
            IEnumerable<string> suppliedIngredientIds, Func<bool> canInteract,
            Action<int> recordServed, Action recordMissed, int activeSlots = 0,
            StaffDefinition staff = null, ShiftSave saved = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (dayNumber < 1) throw new ArgumentOutOfRangeException(nameof(dayNumber));
            if (suppliedIngredientIds == null) throw new ArgumentNullException(nameof(suppliedIngredientIds));
            if (canInteract == null) throw new ArgumentNullException(nameof(canInteract));
            Catalog = catalog;
            DayNumber = dayNumber;
            this.canInteract = canInteract;
            this.recordServed = recordServed ?? throw new ArgumentNullException(nameof(recordServed));
            this.recordMissed = recordMissed ?? throw new ArgumentNullException(nameof(recordMissed));
            supplies = new HashSet<string>(suppliedIngredientIds, StringComparer.Ordinal);
            // BrewingModel validates supplies and adds permanent equipment ingredients.
            brewing = new BrewingModel(catalog, supplies);
            foreach (var id in catalog.IngredientIds)
                if (catalog.GetIngredient(id).alwaysAvailable) supplies.Add(id);
            AvailableRecipeIds = catalog.GetAvailableRecipeIds(dayNumber, supplies);
            var random = new Random();
            if (activeSlots < 0 || activeSlots > catalog.Orders.slotCount) throw new ArgumentOutOfRangeException(nameof(activeSlots));
            orders = new OrderModel[activeSlots == 0 ? catalog.Orders.slotCount : activeSlots];
            for (int i = 0; i < orders.Length; i++)
                orders[i] = new OrderModel(catalog, AvailableRecipeIds, random.Next, i);
            brewing.Changed += () => Changed?.Invoke();
            if (staff != null) Staff = new StaffController(this, staff, new BrewingModel(catalog, supplies));
            if (saved != null)
            {
                if (saved.orders == null || saved.orders.Length != orders.Length || saved.cup == null)
                    throw new InvalidOperationException("Invalid saved shift.");
                for (int i = 0; i < orders.Length; i++) orders[i].Restore(saved.orders[i]);
                foreach (string id in saved.cup)
                    if (brewing.AddIngredient(id) != AddIngredientResult.Added)
                        throw new InvalidOperationException("Invalid saved cup.");
                if (saved.hasStaff && (Staff == null || saved.staff == null)) throw new InvalidOperationException("Saved staff is not hired or task is missing.");
                if (saved.hasStaff) Staff.Restore(saved.staff);
            }
        }

        public ShiftSave Capture()
        {
            var saved = new ShiftSave { cup = new List<string>(Cup).ToArray(), orders = new OrderSave[orders.Length], staff = Staff?.Snapshot, hasStaff = Staff != null };
            for (int i = 0; i < orders.Length; i++) saved.orders[i] = orders[i].Capture();
            return saved;
        }

        public bool HasSupply(string ingredientId) => ingredientId != null && supplies.Contains(ingredientId);

        public void Tick(float deltaSeconds)
        {
            if (!CanInteract) return;
            foreach (var order in orders) order.Tick(deltaSeconds, recordMissed);
            Staff?.Tick(deltaSeconds);
        }

        public void CloseOrders()
        {
            foreach (var order in orders) order.Close();
        }

        public ServeResult TryServe(long orderId) => TryServe(0, orderId);

        public ServeResult TryServe(int slotIndex, long orderId)
            => TryServeCup(slotIndex, orderId, brewing);

        internal ServeResult TryServeCup(int slotIndex, long orderId, BrewingModel cup)
        {
            if (!CanInteract) return ServeResult.InteractionBlocked;
            if (slotIndex < 0 || slotIndex >= orders.Length) return ServeResult.InvalidSlot;
            var order = orders[slotIndex];
            var current = order.Snapshot;
            if (!current.HasCustomer || current.IsClosed) return ServeResult.NoCustomer;
            if (!order.IsCurrent(orderId)) return ServeResult.StaleOrder;
            if (!current.IsSeated) return ServeResult.CustomerArriving;
            if (cup.Cup.Count == 0) return ServeResult.EmptyCup;
            if (cup.CheckRecipe(current.RecipeId) != RecipeMatchResult.Match) return ServeResult.WrongRecipe;
            // Validate and record income before consuming the order or cup.
            recordServed(Catalog.GetRecipe(current.RecipeId).sellPrice);
            order.Complete(orderId);
            cup.ClearCup();
            return ServeResult.Served;
        }

        // False means paused/closed/not in Gameplay. Result otherwise comes from the brewing rules.
        public bool TryAddIngredient(string id, out AddIngredientResult result)
        {
            result = default;
            if (!CanInteract) return false;
            result = brewing.AddIngredient(id);
            return true;
        }

        public bool TryClearCup()
        {
            if (!CanInteract) return false;
            brewing.ClearCup();
            return true;
        }

        public string GetMatchedRecipeId()
        {
            foreach (var id in AvailableRecipeIds)
                if (brewing.CheckRecipe(id) == RecipeMatchResult.Match) return id;
            return null;
        }
    }
}
