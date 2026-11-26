using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    /// <summary>Displays the shared shift. All layout and prefab references are authored in the scene.</summary>
    public sealed class BaristaScreenController : MonoBehaviour
    {
        [Serializable]
        public sealed class IngredientVisual
        {
            public string visualId;
            public Sprite sprite;
        }

        [SerializeField] private IngredientButtonView ingredientPrefab;
        [SerializeField] private Transform ingredientContainer;
        [SerializeField] private IngredientVisual[] ingredientVisuals;
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private Text cupLabel, recipeLabel, feedbackLabel;
        [SerializeField] private Button clearButton;
        private readonly List<IngredientButtonView> buttons = new List<IngredientButtonView>();
        private ShiftController shift;
        public IReadOnlyList<IngredientButtonView> IngredientButtons => buttons.AsReadOnly();

        private void Start()
        {
            clearButton.onClick.AddListener(ClearCup);
            var app = CafeApp.Instance;
            shift = app != null ? app.Shift : null;
            if (shift == null)
            {
                cupLabel.text = "Chưa có ca chơi. Hãy bắt đầu từ MainMenu.";
                clearButton.interactable = false;
                return;
            }
            foreach (var id in shift.Catalog.IngredientIds)
            {
                var definition = shift.Catalog.GetIngredient(id);
                var view = Instantiate(ingredientPrefab, ingredientContainer);
                view.name = "Ingredient-" + id;
                view.Bind(definition, FindSprite(definition.visualId), AddIngredient);
                buttons.Add(view);
            }
            shift.Changed += RefreshCup;
            RefreshRecipes();
            RefreshCup();
            RefreshControls();
        }

        private void Update() => RefreshControls();

        private void RefreshControls()
        {
            bool active = shift != null && shift.CanInteract;
            clearButton.interactable = active && shift.Cup.Count > 0;
            foreach (var button in buttons)
                button.SetInteractable(active && shift.HasSupply(button.IngredientId)
                    && shift.Cup.Count < shift.Catalog.CupCapacity);
        }

        public void AddIngredient(string id)
        {
            if (shift == null || !shift.TryAddIngredient(id, out var result)) return;
            switch (result)
            {
                case AddIngredientResult.CupFull: feedbackLabel.text = "Ly đã đầy. Hãy làm trống ly."; break;
                case AddIngredientResult.NotSupplied: feedbackLabel.text = "Ca này chưa có gói nguyên liệu đó."; break;
                case AddIngredientResult.UnknownIngredient: feedbackLabel.text = "Không tìm thấy nguyên liệu."; break;
            }
            RefreshControls();
        }

        public void ClearCup()
        {
            if (shift != null) shift.TryClearCup();
            RefreshControls();
        }

        private void RefreshCup()
        {
            var text = new StringBuilder();
            foreach (var id in shift.Cup)
            {
                if (text.Length > 0) text.Append("  ›  ");
                text.Append(shift.Catalog.GetIngredient(id).displayName);
            }
            cupLabel.text = text.Length == 0 ? "Ly trống" : text.ToString();
            var match = shift.GetMatchedRecipeId();
            feedbackLabel.text = match != null
                ? shift.Catalog.GetRecipe(match).displayName
                : shift.Cup.Count == 0 ? "" : "Chưa đúng công thức";
        }

        private void RefreshRecipes()
        {
            var text = new StringBuilder("CÔNG THỨC CÓ THỂ PHA\n\n");
            foreach (var id in shift.AvailableRecipeIds)
            {
                var recipe = shift.Catalog.GetRecipe(id);
                text.Append(recipe.displayName).Append("  ·  ").Append(recipe.sellPrice).Append(" xu\n");
                for (int i = 0; i < recipe.ingredientIds.Length; i++)
                {
                    if (i > 0) text.Append(" → ");
                    text.Append(shift.Catalog.GetIngredient(recipe.ingredientIds[i]).displayName);
                }
                text.Append("\n\n");
            }
            recipeLabel.text = text.ToString();
        }

        private Sprite FindSprite(string visualId)
        {
            if (ingredientVisuals != null)
                foreach (var item in ingredientVisuals)
                    if (item != null && item.visualId == visualId && item.sprite != null) return item.sprite;
            Debug.LogWarning("Missing ingredient visual: " + visualId + ". Using fallback/label.", this);
            return fallbackSprite;
        }

        private void OnDestroy()
        {
            if (shift != null) shift.Changed -= RefreshCup;
            if (clearButton != null) clearButton.onClick.RemoveListener(ClearCup);
        }
    }
}
