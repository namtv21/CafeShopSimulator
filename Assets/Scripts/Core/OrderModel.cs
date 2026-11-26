using System;
using System.Collections.Generic;

namespace CafeShopSimulator
{
    public sealed class OrderSnapshot
    {
        public long Id { get; }
        public string RecipeId { get; }
        public float RemainingPatience { get; }
        public float TotalPatience { get; }
        public float ArrivalSeconds { get; }
        public bool IsClosed { get; }
        public bool HasCustomer => RecipeId != null;
        public float WalkRemaining { get; }
        public float WalkDuration { get; }
        public bool IsSeated => HasCustomer && WalkRemaining <= 0;
        public OrderSnapshot(long id, string recipeId, float remaining, float total, float arrival, bool closed, float walkRemaining = 0, float walkDuration = 0)
        {
            Id = id; RecipeId = recipeId; RemainingPatience = remaining;
            TotalPatience = total; ArrivalSeconds = arrival; IsClosed = closed;
            WalkRemaining = walkRemaining; WalkDuration = walkDuration;
        }
    }

    /// <summary>One independent customer position. No scene, UI, wallet or brewing dependencies.</summary>
    public sealed class OrderModel
    {
        private readonly List<string> recipeIds;
        private readonly Func<int, int> chooseIndex;
        private readonly float patienceSeconds, replacementSeconds;
        private float arrivalSeconds, remainingPatience;
        private string recipeId;
        private long sequence;
        private bool closed;
        private readonly float walkDuration;
        private float walkRemaining;
        public OrderSnapshot Snapshot => new OrderSnapshot(sequence, recipeId, remainingPatience,
            patienceSeconds, arrivalSeconds, closed, walkRemaining, walkDuration);

        public OrderModel(ContentCatalog catalog, IReadOnlyList<string> recipeIds, Func<int, int> chooseIndex, int slotIndex = 0)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (recipeIds == null) throw new ArgumentNullException(nameof(recipeIds));
            this.chooseIndex = chooseIndex ?? throw new ArgumentNullException(nameof(chooseIndex));
            this.recipeIds = new List<string>();
            foreach (var id in recipeIds)
            {
                if (!catalog.HasRecipe(id)) throw new ArgumentException("Unknown order recipe: " + id);
                this.recipeIds.Add(id);
            }
            var settings = catalog.Orders;
            if (slotIndex < 0 || slotIndex >= settings.slotCount) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            patienceSeconds = settings.patienceSeconds;
            walkDuration = settings.walkInSeconds;
            replacementSeconds = settings.nextArrivalSeconds;
            arrivalSeconds = settings.firstArrivalSeconds + slotIndex * settings.arrivalStaggerSeconds;
        }

        // One arrival/departure per frame; this is live simulation, not offline fast-forward.
        public void Tick(float deltaSeconds, Action recordMissed)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (recordMissed == null) throw new ArgumentNullException(nameof(recordMissed));
            if (closed || recipeIds.Count == 0 || deltaSeconds == 0) return;
            if (recipeId == null)
            {
                arrivalSeconds = Math.Max(0, arrivalSeconds - deltaSeconds);
                if (arrivalSeconds > 0) return;
                int index = chooseIndex(recipeIds.Count);
                if (index < 0 || index >= recipeIds.Count) throw new InvalidOperationException("Invalid order selection.");
                long next = checked(sequence + 1);
                recipeId = recipeIds[index];
                sequence = next;
                remainingPatience = patienceSeconds;
                walkRemaining = walkDuration;
                return;
            }
            if (walkRemaining > 0) { walkRemaining = Math.Max(0, walkRemaining - deltaSeconds); return; }
            float remaining = Math.Max(0, remainingPatience - deltaSeconds);
            if (remaining > 0) { remainingPatience = remaining; return; }
            // Record first: if accounting fails, keep the customer instead of losing the transaction.
            recordMissed();
            Dismiss();
        }

        public bool IsCurrent(long id) => !closed && recipeId != null && sequence == id;

        public OrderSave Capture() => new OrderSave { id = sequence, recipeId = recipeId,
            patience = remainingPatience, arrival = arrivalSeconds, walkRemaining = walkRemaining, closed = closed };

        public void Restore(OrderSave saved)
        {
            // Unity's inline JSON serializer can round-trip a null string as empty.
            string restoredRecipe = string.IsNullOrEmpty(saved?.recipeId) ? null : saved.recipeId;
            if (saved == null || saved.id < 0 || !ValidTimer(saved.patience) || !ValidTimer(saved.arrival)
                || !ValidTimer(saved.walkRemaining) || saved.patience > patienceSeconds || saved.walkRemaining > walkDuration
                || (restoredRecipe != null && (!recipeIds.Contains(restoredRecipe) || saved.id < 1 || saved.closed || saved.patience <= 0))
                || (restoredRecipe == null && (saved.patience != 0 || saved.walkRemaining != 0)))
                throw new InvalidOperationException("Invalid saved customer.");
            sequence = saved.id; recipeId = restoredRecipe; remainingPatience = saved.patience;
            arrivalSeconds = saved.arrival; walkRemaining = saved.walkRemaining; closed = saved.closed;
        }
        private static bool ValidTimer(float value) => value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);

        public void Complete(long id)
        {
            if (!IsCurrent(id)) throw new InvalidOperationException("Order is no longer active.");
            Dismiss();
        }

        private void Dismiss()
        {
            recipeId = null;
            walkRemaining = 0;
            remainingPatience = 0;
            arrivalSeconds = replacementSeconds;
        }

        public void Close()
        {
            closed = true;
            recipeId = null;
            remainingPatience = arrivalSeconds = 0;
            walkRemaining = 0;
        }
    }
}
