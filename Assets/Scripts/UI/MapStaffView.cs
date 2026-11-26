using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    /// <summary>Projects the saved staff task onto an authored aisle route; never awards income.</summary>
    public sealed class MapStaffView : MonoBehaviour
    {
        public SpriteRenderer body;
        public Sprite[] walkLeft, walkRight, walkUp, walkDown;
        public Transform workstation, counterAisle;
        public Transform[] tableAisles, servicePoints;
        public Text status;
        public SpriteRenderer carriedItem;
        public Sprite drinkSprite, foodSprite;
        private float animationTime;
        private string characterId;

        private void Update()
        {
            var shift = CafeApp.Instance?.Shift;
            var staff = shift?.Staff;
            body.enabled = staff != null;
            if (carriedItem != null) carriedItem.enabled = false;
            if (staff == null) { if (status != null) status.text = "Chưa có trợ lý · Thuê tại Xưởng"; return; }
            if (characterId != staff.Definition.id)
            {
                var definition = CafeCharacterCatalog.Load()?.Find(staff.Definition.id);
                if (definition != null)
                {
                    walkLeft = definition.left; walkRight = definition.right;
                    walkUp = definition.up; walkDown = definition.down;
                    body.color = Color.white;
                }
                characterId = staff.Definition.id;
            }
            var task = staff.Snapshot;
            Vector3 previous = transform.position;
            float progress = 0;
            if (task.phase == StaffPhase.Delivering) progress = 1 - task.remainingSeconds / staff.Definition.deliverySeconds;
            if (task.phase == StaffPhase.Returning) progress = task.remainingSeconds / staff.Definition.deliverySeconds;
            transform.position = task.slot >= 0 && task.slot < servicePoints.Length
                ? Position(task.slot, progress) : workstation.position;
            if (shift.CanInteract) animationTime += Time.deltaTime;
            Vector3 direction = transform.position - previous;
            var frames = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? (direction.x > 0 ? walkRight : walkLeft) : (direction.y > 0 ? walkUp : walkDown);
            bool moving = direction.sqrMagnitude >= .000001f;
            if (!moving) frames = walkDown;
            if (frames != null && frames.Length > 0) body.sprite = frames[moving ? (int)(animationTime * 7) % frames.Length : Mathf.Min(1, frames.Length - 1)];
            body.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
            if (carriedItem != null && task.phase == StaffPhase.Delivering)
            {
                carriedItem.sprite = task.recipeId == "butter-cookie" ? foodSprite : drinkSprite;
                carriedItem.enabled = carriedItem.sprite != null;
                carriedItem.sortingOrder = body.sortingOrder + 1;
                carriedItem.transform.localPosition = new Vector3(direction.x < -.001f ? -.42f : .42f, .3f);
            }
            if (status != null)
            {
                string action = task.phase == StaffPhase.Brewing ? "Đang pha"
                    : task.phase == StaffPhase.Delivering ? "Mang đồ → bàn " + (task.slot + 1)
                    : task.phase == StaffPhase.Returning ? "Về quầy" : "Chờ khách";
                status.text = staff.Definition.name + " · " + action;
            }
        }

        private Vector3 Position(int slot, float t)
        {
            var a = workstation.position; var b = counterAisle.position;
            var c = tableAisles[slot].position; var d = servicePoints[slot].position;
            float ab = Vector3.Distance(a, b), bc = Vector3.Distance(b, c), cd = Vector3.Distance(c, d);
            float distance = Mathf.Clamp01(t) * (ab + bc + cd);
            if (distance <= ab) return Vector3.Lerp(a, b, ab > 0 ? distance / ab : 1);
            distance -= ab;
            if (distance <= bc) return Vector3.Lerp(b, c, bc > 0 ? distance / bc : 1);
            return Vector3.Lerp(c, d, cd > 0 ? (distance - bc) / cd : 1);
        }
    }
}
