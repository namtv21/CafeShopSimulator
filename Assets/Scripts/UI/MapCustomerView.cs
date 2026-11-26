using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    /// <summary>One fixed table observes one order. Visual movement cannot award money or create orders.</summary>
    public sealed class MapCustomerView : MonoBehaviour
    {
        [Serializable] public struct DrinkIcon { public string id; public Sprite sprite; }
        public int slot;
        public SpriteRenderer body;
        public Sprite[] walkLeft, walkRight, walkUp, walkDown;
        public Sprite seated;
        public Transform[] route;
        public GameObject bubble;
        public Image drink;
        public Text drinkName;
        public DrinkIcon[] drinkIcons;
        private long lastId;
        private bool departing;
        private float animationTime;
        private ShiftController Shift => CafeApp.Instance?.Shift;
        public bool IsDeparting => departing;
        private void Update()
        {
            var shift=Shift;
            if(shift==null || slot>=shift.OrderCount) { body.enabled=false;bubble.SetActive(false);return; }
            var order=shift.GetOrder(slot);
            if(order.HasCustomer)
            {
                departing=false;body.enabled=true;
                if(lastId!=order.Id)
                {
                    lastId=order.Id;drinkName.text=shift.Catalog.GetRecipe(order.RecipeId).displayName;
                    drink.sprite=null;
                    foreach(var icon in drinkIcons)if(icon.id==order.RecipeId){drink.sprite=icon.sprite;break;}
                    drink.gameObject.SetActive(drink.sprite!=null);
                }
                float t=order.WalkDuration<=0?1:1-order.WalkRemaining/order.WalkDuration;
                MoveAlong(t,shift.CanInteract);
                bubble.SetActive(order.IsSeated);
                if(order.IsSeated)body.sprite=seated;
            }
            else
            {
                bubble.SetActive(false);
                // The replacement timer is also the return walk. Reading model time makes
                // switching POV, pausing and loading mid-exit display the same customer position.
                float duration=shift.Catalog.Orders.nextArrivalSeconds;
                departing=!order.IsClosed && order.Id>0 && order.ArrivalSeconds>0 && duration>0;
                body.enabled=departing;
                if(departing)
                {
                    float handoffTime = Mathf.Min(.65f, duration * .25f);
                    float exitProgress = Mathf.Clamp01((duration - order.ArrivalSeconds - handoffTime) / (duration - handoffTime));
                    Vector3 old=transform.position;
                    // Return along the same clear aisle; never cross a table diagonally.
                    transform.position=exitProgress<.5f?Vector3.Lerp(route[2].position,route[1].position,exitProgress*2)
                        :Vector3.Lerp(route[1].position,route[0].position,(exitProgress-.5f)*2);
                    if(shift.CanInteract)Animate(transform.position-old);
                    if (exitProgress == 0) body.sprite = seated;
                }
            }
            body.sortingOrder=100-Mathf.RoundToInt(transform.position.y*10);
        }
        private void MoveAlong(float t,bool animate)
        {
            Vector3 old=transform.position;
            transform.position=t<.5f?Vector3.Lerp(route[0].position,route[1].position,t*2)
                :Vector3.Lerp(route[1].position,route[2].position,(t-.5f)*2);
            if(animate)Animate(transform.position-old);
        }
        private void Animate(Vector3 direction)
        {
            if(direction.sqrMagnitude<.000001f)return;
            animationTime+=Time.deltaTime;
            var frames=Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?(direction.x>0?walkRight:walkLeft):(direction.y>0?walkUp:walkDown);
            if(frames.Length>0)body.sprite=frames[(int)(animationTime*9)%frames.Length];
        }
    }
}
