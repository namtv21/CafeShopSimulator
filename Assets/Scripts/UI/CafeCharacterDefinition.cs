using UnityEngine;

namespace CafeShopSimulator
{
    public enum CafeCharacterRole { Unassigned, Owner, Waitress }

    [CreateAssetMenu(menuName = "Cafe/Character")]
    public sealed class CafeCharacterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public CafeCharacterRole role;
        public Sprite[] down, left, right, up;
        public Sprite Idle => down != null && down.Length > 1 ? down[1] : null;
        public Sprite[] Frames(Vector2 direction)
        {
            return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? (direction.x > 0 ? right : left) : (direction.y > 0 ? up : down);
        }
    }
}
