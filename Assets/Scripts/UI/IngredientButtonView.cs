using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    [RequireComponent(typeof(Button))]
    public sealed class IngredientButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Image iconImage;
        private Action<string> clicked;
        public string IngredientId { get; private set; }
        public Button Button => button;

        public void Bind(IngredientDefinition ingredient, Sprite sprite, Action<string> onClicked)
        {
            IngredientId = ingredient.id;
            clicked = onClicked;
            nameLabel.text = ingredient.displayName;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        public void SetInteractable(bool value) => button.interactable = value;
        private void OnClicked() => clicked?.Invoke(IngredientId);
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            clicked = null;
        }
    }
}
