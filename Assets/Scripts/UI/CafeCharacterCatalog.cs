using System;
using UnityEngine;

namespace CafeShopSimulator
{
    [CreateAssetMenu(menuName = "Cafe/Character Catalog")]
    public sealed class CafeCharacterCatalog : ScriptableObject
    {
        public CafeCharacterDefinition[] characters;
        public CafeCharacterDefinition Find(string id) => Array.Find(characters, c => c != null && c.id == id);
        private static CafeCharacterCatalog loaded;
        public static CafeCharacterCatalog Load() => loaded != null ? loaded : (loaded = Resources.Load<CafeCharacterCatalog>("Cafe/character-catalog"));
    }
}
