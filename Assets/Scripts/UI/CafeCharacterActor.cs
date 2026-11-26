using UnityEngine;

namespace CafeShopSimulator
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CafeCharacterActor : MonoBehaviour
    {
        public CafeCharacterDefinition character;
        public bool animatePreview;
        private SpriteRenderer body;
        private Vector3 previous;
        private Vector2 facing = Vector2.down;
        private float clock;
        private void OnEnable()
        {
            body = GetComponent<SpriteRenderer>(); previous = transform.position;
            if (character != null) body.sprite = character.Idle;
        }
        private void LateUpdate()
        {
            if (character == null) return;
            var motion = (Vector2)(transform.position - previous); previous = transform.position;
            bool moving = motion.sqrMagnitude > .000001f;
            bool paused = CafeApp.Instance != null && CafeApp.Instance.IsPaused;
            if (!paused) clock += Time.deltaTime;
            if (moving) facing = motion;
            if (animatePreview) facing = new[] { Vector2.down, Vector2.left, Vector2.right, Vector2.up }[(int)(clock / 2) % 4];
            var frames = character.Frames(facing);
            if (frames != null && frames.Length > 0) body.sprite = frames[moving || animatePreview ? (int)(clock * 7) % frames.Length : 1];
            body.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
        }
    }
}
