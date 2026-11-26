using System;

namespace CafeShopSimulator
{
    // Serializable DTOs: field names match game-content.json and Unity JsonUtility.
    [Serializable]
    public sealed class GameContent
    {
        public int version = 1;
        public int cupCapacity = 5;
        public OrderSettings orders = new OrderSettings();
        public IngredientDefinition[] ingredients;
        public RecipeDefinition[] recipes;
    }

    [Serializable]
    public sealed class OrderSettings
    {
        public int slotCount = 3;
        public float arrivalStaggerSeconds = 1.5f;
        public float firstArrivalSeconds = 2;
        public float nextArrivalSeconds = 3;
        public float patienceSeconds = 25;
        public float walkInSeconds;
    }

    [Serializable]
    public sealed class IngredientDefinition
    {
        public string id;
        public string displayName;
        public string visualId;
        public int packPrice;
        public bool alwaysAvailable;
        public bool freeOnFirstDay;
    }

    [Serializable]
    public sealed class RecipeDefinition
    {
        public string id;
        public string displayName;
        public string visualId;
        public string[] ingredientIds;
        public int sellPrice;
        // Recipes unlock automatically; recipes are never shop purchases.
        public int unlockDay = 1;
    }
}
