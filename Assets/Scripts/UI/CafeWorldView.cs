using UnityEngine;

namespace CafeShopSimulator
{
    public sealed class CafeWorldView : MonoBehaviour
    {
        public GameObject world;
        public GameObject[] tables;
        public CafeScreenUI ui;
        public SpriteRenderer entranceDoor;
        public MapCustomerView[] customers;
        private void Update()
        {
            if (ui != null && ui.roomView != null) world.SetActive(ui.roomView.activeSelf);
            if (CafeApp.Instance?.Shift == null) return;
            for(int i=0;i<tables.Length;i++) tables[i].SetActive(i<CafeApp.Instance.Shift.OrderCount);
        }

        private void LateUpdate()
        {
            if (entranceDoor == null || !world.activeInHierarchy || customers == null) return;
            Vector3 threshold = entranceDoor.transform.position;
            bool crossing = false;
            foreach (var customer in customers)
            {
                if (!customer.isActiveAndEnabled || !customer.body.enabled) continue;
                Vector3 position = customer.transform.position;
                if (Mathf.Abs(position.x - threshold.x) < .65f && Mathf.Abs(position.y - threshold.y) < 1.15f)
                {
                    crossing = true;
                    break;
                }
            }
            entranceDoor.enabled = !crossing;
        }
    }
}
