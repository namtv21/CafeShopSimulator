using System;
using System.Collections.Generic;

namespace CafeShopSimulator
{
    /// <summary>Validates content once and owns defensive copies of its definitions.</summary>
    public sealed class ContentCatalog
    {
        private readonly Dictionary<string, IngredientDefinition> ingredients =
            new Dictionary<string, IngredientDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, RecipeDefinition> recipes =
            new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
        private readonly List<string> ingredientIds = new List<string>();
        private readonly List<string> recipeIds = new List<string>();

        public int CupCapacity { get; private set; }
        private readonly OrderSettings orderSettings;
        public OrderSettings Orders => new OrderSettings {
            slotCount = orderSettings.slotCount,
            arrivalStaggerSeconds = orderSettings.arrivalStaggerSeconds,
            firstArrivalSeconds = orderSettings.firstArrivalSeconds,
            nextArrivalSeconds = orderSettings.nextArrivalSeconds,
            walkInSeconds = orderSettings.walkInSeconds,
            patienceSeconds = orderSettings.patienceSeconds };
        public IReadOnlyList<string> IngredientIds { get { return ingredientIds.AsReadOnly(); } }
        public IReadOnlyList<string> RecipeIds { get { return recipeIds.AsReadOnly(); } }

        public ContentCatalog(GameContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.version != 1 || content.cupCapacity < 1)
                throw new ArgumentException("Unsupported content version or invalid cup capacity.");
            if (content.ingredients == null || content.ingredients.Length == 0 ||
                content.recipes == null || content.recipes.Length == 0)
                throw new ArgumentException("Content must contain ingredients and recipes.");
            CupCapacity = content.cupCapacity;
            var timing = content.orders;
            if (timing == null || timing.slotCount < 1 || !ValidSeconds(timing.walkInSeconds, true) || !ValidSeconds(timing.arrivalStaggerSeconds, true)
                || !ValidSeconds(timing.firstArrivalSeconds + (timing.slotCount - 1) * timing.arrivalStaggerSeconds, true)
                || !ValidSeconds(timing.firstArrivalSeconds, true)
                || !ValidSeconds(timing.nextArrivalSeconds, false) || !ValidSeconds(timing.patienceSeconds, false))
                throw new ArgumentException("Order timings must be finite; arrival >= 0, replacement and patience > 0.");
            orderSettings = new OrderSettings { slotCount = timing.slotCount, arrivalStaggerSeconds = timing.arrivalStaggerSeconds,
                walkInSeconds = timing.walkInSeconds,
                firstArrivalSeconds = timing.firstArrivalSeconds,
                nextArrivalSeconds = timing.nextArrivalSeconds, patienceSeconds = timing.patienceSeconds };
            foreach (var item in content.ingredients)
            {
                if (item == null) throw new ArgumentException("Null ingredient.");
                ValidateIdentity(item.id, item.displayName);
                if (item.packPrice < 0 || ingredients.ContainsKey(item.id))
                    throw new ArgumentException("Invalid price or duplicate ingredient: " + item.id);
                ingredients.Add(item.id, Copy(item));
                ingredientIds.Add(item.id);
            }
            foreach (var recipe in content.recipes)
            {
                if (recipe == null) throw new ArgumentException("Null recipe.");
                ValidateIdentity(recipe.id, recipe.displayName);
                if (recipes.ContainsKey(recipe.id) || recipe.sellPrice < 0 || recipe.unlockDay < 1)
                    throw new ArgumentException("Invalid price, unlock day or duplicate recipe: " + recipe.id);
                if (recipe.ingredientIds == null || recipe.ingredientIds.Length == 0 ||
                    recipe.ingredientIds.Length > CupCapacity)
                    throw new ArgumentException("Recipe must fit in the cup: " + recipe.id);
                foreach (var id in recipe.ingredientIds)
                    if (id == null || !ingredients.ContainsKey(id))
                        throw new ArgumentException("Unknown ingredient in recipe " + recipe.id + ": " + id);
                recipes.Add(recipe.id, Copy(recipe));
                recipeIds.Add(recipe.id);
            }
        }

        public bool HasIngredient(string id) { return id != null && ingredients.ContainsKey(id); }
        public bool HasRecipe(string id) { return id != null && recipes.ContainsKey(id); }

        public IngredientDefinition GetIngredient(string id)
        {
            if (!HasIngredient(id)) throw new ArgumentException("Unknown ingredient: " + id);
            return Copy(ingredients[id]);
        }

        public RecipeDefinition GetRecipe(string id)
        {
            if (!HasRecipe(id)) throw new ArgumentException("Unknown recipe: " + id);
            return Copy(recipes[id]);
        }

        public IReadOnlyList<string> GetAvailableRecipeIds(int dayNumber, IEnumerable<string> suppliedIngredientIds)
        {
            if (dayNumber < 1) throw new ArgumentOutOfRangeException(nameof(dayNumber));
            if (suppliedIngredientIds == null) throw new ArgumentNullException(nameof(suppliedIngredientIds));
            var supplies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in suppliedIngredientIds)
            {
                if (!HasIngredient(id)) throw new ArgumentException("Unknown supplied ingredient: " + id);
                supplies.Add(id);
            }
            foreach (var pair in ingredients)
                if (pair.Value.alwaysAvailable) supplies.Add(pair.Key);
            var available = new List<string>();
            foreach (var id in recipeIds)
            {
                var recipe = recipes[id];
                if (recipe.unlockDay > dayNumber) continue;
                bool canMake = true;
                foreach (var ingredient in recipe.ingredientIds)
                    if (!supplies.Contains(ingredient)) { canMake = false; break; }
                if (canMake) available.Add(id);
            }
            return available.AsReadOnly();
        }

        private static void ValidateIdentity(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("IDs must be nonempty without surrounding spaces; names are required.");
        }

        private static bool ValidSeconds(float value, bool allowZero)
            => !float.IsNaN(value) && !float.IsInfinity(value) && (allowZero ? value >= 0 : value > 0);

        private static IngredientDefinition Copy(IngredientDefinition value)
        {
            return new IngredientDefinition { id = value.id, displayName = value.displayName,
                visualId = value.visualId, packPrice = value.packPrice,
                alwaysAvailable = value.alwaysAvailable, freeOnFirstDay = value.freeOnFirstDay };
        }

        private static RecipeDefinition Copy(RecipeDefinition value)
        {
            return new RecipeDefinition { id = value.id, displayName = value.displayName,
                visualId = value.visualId, ingredientIds = (string[])value.ingredientIds.Clone(),
                sellPrice = value.sellPrice, unlockDay = value.unlockDay };
        }
    }
}
