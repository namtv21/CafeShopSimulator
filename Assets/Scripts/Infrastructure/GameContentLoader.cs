using System;
using UnityEngine;

namespace CafeShopSimulator
{
    public static class GameContentLoader
    {
        public static ContentCatalog Load(TextAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            return Parse(asset.text);
        }

        public static ContentCatalog Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Game content JSON is empty.");
            return new ContentCatalog(JsonUtility.FromJson<GameContent>(json));
        }
    }
}
