using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    public sealed class HubItemView : MonoBehaviour
    {
        public Text title, detail, price;
        public Image icon;
        public Button button;
        public void Bind(string name, string description, string actionLabel, bool enabled, Sprite sprite, Action action)
        {
            title.text = name; detail.text = description; price.text = actionLabel;
            icon.sprite = sprite; icon.gameObject.SetActive(sprite != null);
            button.interactable = enabled;
            button.onClick.AddListener(() => action?.Invoke());
        }
    }
}
