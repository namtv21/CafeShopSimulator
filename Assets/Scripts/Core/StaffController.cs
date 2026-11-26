using System;

namespace CafeShopSimulator
{
    [Serializable]
    public sealed class StaffDefinition
    {
        public string id, name, description;
        public int hirePrice;
        public float brewSeconds = 8, deliverySeconds = 3;
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || hirePrice < 0
                || !ValidTime(brewSeconds) || !ValidTime(deliverySeconds))
                throw new ArgumentException("Invalid staff definition.");
        }
        private static bool ValidTime(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
    }

    public enum StaffPhase { Idle, Brewing, Delivering, Returning }

    [Serializable]
    public sealed class StaffSave
    {
        public StaffPhase phase;
        public int slot = -1;
        public long orderId;
        public string recipeId;
        public float remainingSeconds;
        public StaffSave Copy() => (StaffSave)MemberwiseClone();
    }

    /// <summary>One assistant uses a separate cup and the same serving transaction as the player.</summary>
    public sealed class StaffController
    {
        private readonly ShiftController shift;
        private readonly BrewingModel cup;
        private StaffSave state = new StaffSave();
        public StaffDefinition Definition { get; }
        public StaffSave Snapshot => state.Copy();

        public StaffController(ShiftController shift, StaffDefinition definition, BrewingModel cup)
        {
            this.shift = shift; Definition = definition; this.cup = cup;
            definition.Validate();
        }

        public void Restore(StaffSave saved)
        {
            if (saved == null) return;
            if (!Enum.IsDefined(typeof(StaffPhase), saved.phase) || saved.remainingSeconds < 0
                || float.IsNaN(saved.remainingSeconds) || float.IsInfinity(saved.remainingSeconds)
                || (saved.phase != StaffPhase.Idle && (saved.slot < 0 || saved.slot >= shift.OrderCount || saved.orderId < 1))
                || (saved.phase == StaffPhase.Idle && (saved.slot != -1 || saved.remainingSeconds != 0))
                || saved.remainingSeconds > (saved.phase == StaffPhase.Brewing ? Definition.brewSeconds : Definition.deliverySeconds))
                throw new InvalidOperationException("Invalid saved staff task.");
            state = saved.Copy();
            if (state.phase == StaffPhase.Brewing || state.phase == StaffPhase.Delivering)
            {
                if (!System.Linq.Enumerable.Contains(shift.AvailableRecipeIds, state.recipeId))
                    throw new InvalidOperationException("Saved staff recipe is unavailable.");
                PrepareCup();
            }
        }

        public void Tick(float delta)
        {
            if (delta <= 0) return;
            if (state.phase == StaffPhase.Idle)
            {
                // Oldest / least patient seated customer first; stable slot tie-break.
                float patience = float.MaxValue;
                for (int i = 0; i < shift.OrderCount; i++)
                {
                    var order = shift.GetOrder(i);
                    if (!order.IsClosed && order.IsSeated && order.RemainingPatience < patience)
                    { state.slot = i; state.orderId = order.Id; patience = order.RemainingPatience; }
                }
                if (state.slot < 0) return;
                state.recipeId = shift.GetOrder(state.slot).RecipeId;
                PrepareCup(); state.phase = StaffPhase.Brewing; state.remainingSeconds = Definition.brewSeconds;
                return;
            }
            if ((state.phase == StaffPhase.Brewing || state.phase == StaffPhase.Delivering) && !HasTarget())
            {
                // Complete the delivery route visually before returning, even if the player served first.
                if (state.phase == StaffPhase.Brewing) { Reset(); return; }
            }
            state.remainingSeconds = Math.Max(0, state.remainingSeconds - delta);
            if (state.remainingSeconds > 0) return;
            switch (state.phase)
            {
                case StaffPhase.Brewing:
                    state.phase = StaffPhase.Delivering; state.remainingSeconds = Definition.deliverySeconds; break;
                case StaffPhase.Delivering:
                    shift.TryServeCup(state.slot, state.orderId, cup);
                    cup.ClearCup(); state.phase = StaffPhase.Returning; state.remainingSeconds = Definition.deliverySeconds; break;
                case StaffPhase.Returning: Reset(); break;
            }
        }

        private bool HasTarget()
        {
            var order = shift.GetOrder(state.slot);
            return !order.IsClosed && order.IsSeated && order.Id == state.orderId;
        }
        private void PrepareCup()
        {
            cup.ClearCup();
            foreach (string id in shift.Catalog.GetRecipe(state.recipeId).ingredientIds)
                if (cup.AddIngredient(id) != AddIngredientResult.Added)
                    throw new InvalidOperationException("Staff cannot brew an unavailable recipe.");
        }
        private void Reset() { cup.ClearCup(); state = new StaffSave(); }
    }
}
