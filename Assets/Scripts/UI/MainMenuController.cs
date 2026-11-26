using UnityEngine;
using UnityEngine.Events;

namespace CafeShopSimulator
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UnityEvent optionsRequested = new UnityEvent();
        public bool CanLoad => CafeApp.Instance != null && CafeApp.Instance.Saves.HasSave;
        public void NewGame() => CafeApp.Instance.NewGame();
        public void LoadGame() => CafeApp.Instance.LoadGame();
        public void OpenOptions() => optionsRequested?.Invoke();
    }
}
