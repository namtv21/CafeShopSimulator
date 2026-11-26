using UnityEngine;

namespace CafeShopSimulator
{
    /// <summary>Input boundary; brewing logic will call these only after validating a serve.</summary>
    public sealed class GameplayController : MonoBehaviour
    {
        public CafeProgress Snapshot => CafeApp.Instance.Days?.Snapshot;
        public void Pause() => CafeApp.Instance.SetPaused(true);
        public void Resume() => CafeApp.Instance.SetPaused(false);
        public void MainMenu() => CafeApp.Instance.ReturnToMainMenu();
        public void RetrySave() => CafeApp.Instance.RetryEndDaySave();
        public void RecordServed(int income)
        {
            if (!CafeApp.Instance.IsPaused) CafeApp.Instance.Days.RecordServed(income);
        }
        public void RecordMissed()
        {
            if (!CafeApp.Instance.IsPaused) CafeApp.Instance.Days.RecordMissed();
        }
    }
}
