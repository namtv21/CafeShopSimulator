using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeShopSimulator
{
    /// <summary>Bindings for authored scene UI. Does not construct UI at runtime.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class CafeScreenUI : MonoBehaviour
    {
        public enum ScreenKind { MainMenu, DayHub, Gameplay, AfterShift, Town }
        public ScreenKind screenKind;
        public Text dayLabel, walletLabel, timerLabel, incomeLabel, servedLabel, missedLabel;
        public Text titleLabel, subtitleLabel, messageLabel, previewLabel;
        public Button loadButton, primaryButton, retryButton;
        public Text primaryButtonLabel;
        public GameObject confirmationPanel, optionsPanel, pausePanel;
        public GameObject povView, roomView, lockedRoomPanel;
        public GameObject recipeBook;
        public void ToggleRecipes() { if (recipeBook != null) recipeBook.SetActive(!recipeBook.activeSelf); }
        public Slider volumeSlider;
        public Toggle fullscreenToggle;
        private CafeApp app;
        private bool optionsWerePaused;
        private bool lockedRoomWasPaused;
        private bool pauseWasPaused;

        private void Start()
        {
            app = CafeApp.Instance;
            if (app == null)
            {
                var root = new GameObject("CafeApp");
                app = root.AddComponent<CafeApp>();
            }
            app.Error += ShowError;
            AudioListener.volume = PlayerPrefs.GetFloat("cafe.volume", 0.8f);
            if (PlayerPrefs.HasKey("cafe.fullscreen"))
                Screen.fullScreen = PlayerPrefs.GetInt("cafe.fullscreen") == 1;
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            if (app.Days == null && (screenKind == ScreenKind.Town || screenKind == ScreenKind.Gameplay))
                app.BeginQuickTest(screenKind == ScreenKind.Gameplay);
            if (screenKind != ScreenKind.MainMenu && app.Days == null)
                ShowError("Chưa có phiên chơi. Về menu để chọn Chơi mới hoặc Tiếp tục.");
            Refresh();
        }

        private void Update() => Refresh();

        private void OnDestroy()
        {
            if (app != null) app.Error -= ShowError;
        }

        private void Refresh()
        {
            if (app == null) return;
            if (loadButton != null) loadButton.interactable = app.Saves.HasSave;
            var state = app.Days?.Snapshot;
            if (state == null)
            {
                if (primaryButton != null) primaryButton.interactable = false;
                return;
            }
            if (dayLabel != null) dayLabel.text = $"NGÀY {state.dayNumber:00}";
            if (walletLabel != null) walletLabel.text = $"{state.wallet:N0} xu";
            if (incomeLabel != null) incomeLabel.text = $"+{state.runIncome:N0} xu";
            if (servedLabel != null) servedLabel.text = state.servedCustomers.ToString("00");
            if (missedLabel != null) missedLabel.text = state.missedCustomers.ToString("00");
            if (timerLabel != null)
            {
                int seconds = Mathf.CeilToInt(state.remainingSeconds);
                timerLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
            if (screenKind == ScreenKind.DayHub || screenKind == ScreenKind.AfterShift)
            {
                bool summary = screenKind == ScreenKind.AfterShift;
                if (titleLabel != null) titleLabel.text = summary ? "SAU CA" : "TRƯỚC CA";
                if (subtitleLabel != null) subtitleLabel.text = "";
                if (primaryButtonLabel != null) primaryButtonLabel.text = summary ? "Về phố" : "Mở cửa";
                if (primaryButton != null) primaryButton.interactable = summary
                    ? state.phase == DayPhase.Summary : state.phase == DayPhase.Preparation;
            }
            if (retryButton != null) retryButton.gameObject.SetActive(state.phase == DayPhase.Summary);
        }

        public void RequestNewGame()
        {
            if (app == null) return;
            if (app.Saves.HasSave) confirmationPanel.SetActive(true);
            else app.NewGame();
        }
        public void ConfirmNewGame() { confirmationPanel.SetActive(false); app.NewGame(); }
        public void CancelNewGame() => confirmationPanel.SetActive(false);
        public void LoadGame() => app.LoadGame();
        public void PrimaryAction()
        {
            if (app.Days == null) return;
            if (app.Days.Snapshot.phase == DayPhase.Summary) app.NextDay();
            else app.StartDay();
            Refresh();
        }
        public void MainMenu() => app.ReturnToMainMenu();
        public void RetrySave() => app.RetryEndDaySave();
        public void OpenOptions()
        {
            optionsWerePaused = app.IsPaused;
            app.SetPaused(true);
            optionsPanel.SetActive(true);
        }
        public void CloseOptions()
        {
            optionsPanel.SetActive(false);
            app.SetPaused(optionsWerePaused);
            PlayerPrefs.Save();
        }
        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("cafe.volume", value);
        }
        public void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt("cafe.fullscreen", value ? 1 : 0);
        }
        public void Pause()
        {
            if (app == null || app.Days == null || app.Days.Snapshot.phase != DayPhase.Playing) return;
            pauseWasPaused = app.IsPaused;
            app.SetPaused(true);
            pausePanel.SetActive(true);
        }
        public void Resume() { pausePanel.SetActive(false); app.SetPaused(pauseWasPaused); }
        public void ShowRoom()
        {
            if (roomView.activeSelf) return;
            roomView.SetActive(true);
            povView.SetActive(false);
        }
        public void ShowPov()
        {
            roomView.SetActive(false);
            povView.SetActive(true);
        }
        public void ShowBackroom()
        {
            var hubView = GetComponent<CafeHubUI>();
            if (hubView != null) { hubView.OpenBackroom(); return; }
            lockedRoomWasPaused = app.IsPaused;
            app.SetPaused(true);
            lockedRoomPanel.SetActive(true);
        }
        public void CloseBackroom()
        {
            lockedRoomPanel.SetActive(false);
            app.SetPaused(lockedRoomWasPaused);
        }
        private void ShowError(string message)
        {
            if (messageLabel != null) messageLabel.text = message;
        }
    }
}
