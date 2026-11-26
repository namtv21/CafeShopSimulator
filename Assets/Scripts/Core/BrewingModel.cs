using System;
using System.Collections.Generic;

namespace CafeShopSimulator
{
    public enum AddIngredientResult { Added, UnknownIngredient, NotSupplied, CupFull }
    public enum RecipeMatchResult { Match, EmptyCup, UnknownRecipe, WrongSequence }

    /// <summary>One cup per shift. Supply packs do not deplete as drinks are made.</summary>
    public sealed class BrewingModel
    {
        private readonly ContentCatalog catalog;
        private readonly HashSet<string> supplies = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> cup = new List<string>();
        public IReadOnlyList<string> Cup { get { return cup.AsReadOnly(); } }
        public event Action Changed;

        public BrewingModel(ContentCatalog catalog, IEnumerable<string> suppliedIngredientIds)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (suppliedIngredientIds == null) throw new ArgumentNullException(nameof(suppliedIngredientIds));
            this.catalog = catalog;
            foreach (var id in suppliedIngredientIds)
            {
                if (!catalog.HasIngredient(id)) throw new ArgumentException("Unknown supply: " + id);
                supplies.Add(id);
            }
            foreach (var id in catalog.IngredientIds)
                if (catalog.GetIngredient(id).alwaysAvailable) supplies.Add(id);
        }

        public AddIngredientResult AddIngredient(string id)
        {
            if (!catalog.HasIngredient(id)) return AddIngredientResult.UnknownIngredient;
            if (!supplies.Contains(id)) return AddIngredientResult.NotSupplied;
            if (cup.Count >= catalog.CupCapacity) return AddIngredientResult.CupFull;
            cup.Add(id);
            Changed?.Invoke();
            return AddIngredientResult.Added;
        }

        // Checking does not consume the cup or grant money. Serving is an order-system operation.
        public RecipeMatchResult CheckRecipe(string recipeId)
        {
            if (!catalog.HasRecipe(recipeId)) return RecipeMatchResult.UnknownRecipe;
            if (cup.Count == 0) return RecipeMatchResult.EmptyCup;
            var expected = catalog.GetRecipe(recipeId).ingredientIds;
            if (expected.Length != cup.Count) return RecipeMatchResult.WrongSequence;
            for (int i = 0; i < expected.Length; i++)
                if (!string.Equals(expected[i], cup[i], StringComparison.Ordinal))
                    return RecipeMatchResult.WrongSequence;
            return RecipeMatchResult.Match;
        }

        public void ClearCup()
        {
            if (cup.Count == 0) return;
            cup.Clear();
            Changed?.Invoke();
        }
    }
}
