using UnityEngine;

namespace CafeShopSimulator
{
    public sealed class DayHubController : MonoBehaviour
    {
        public CafeProgress Snapshot => CafeApp.Instance.Days?.Snapshot;
        public bool IsSummary => Snapshot != null && Snapshot.phase == DayPhase.Summary;
        public void StartDay() => CafeApp.Instance.StartDay();
        public void NextDay() => CafeApp.Instance.NextDay();
        public void MainMenu() => CafeApp.Instance.ReturnToMainMenu();
    }
}
