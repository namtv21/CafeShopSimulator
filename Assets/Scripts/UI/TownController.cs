using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    public sealed class TownController : MonoBehaviour
    {
        public CafeHubUI hub;
        public GameObject notice;
        public Text noticeText;
        public void EnterCafe() => CafeApp.Instance.StartDay();
        public void Shop() => hub.OpenShop();
        public void Workshop() => hub.OpenUpgrades();
        public void Talk() => hub.OpenChat();
        public void EmptyLot() { noticeText.text="Khu đất mở chi nhánh\nChưa mở trong phiên bản này.";notice.SetActive(true); }
        public void CloseNotice() => notice.SetActive(false);
        public void InspectLandmark(string description)
        {
            noticeText.text = description;
            notice.SetActive(true);
        }
    }
}
