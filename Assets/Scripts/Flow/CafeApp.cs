using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CafeShopSimulator
{
    /// <summary>Persistent root: preparation -> active shift -> after-shift results.</summary>
    public sealed class CafeApp : MonoBehaviour
    {
        public static CafeApp Instance { get; private set; }
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string dayHubScene = "DayHub";
        [SerializeField] private string townScene = "Town";
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private string afterShiftScene = "AfterShift";
        [SerializeField, Min(0)] private int startingCoins = 100;
        [SerializeField, Min(1)] private float dayDurationSeconds = 120;
        public DayService Days { get; private set; }
        public SaveService Saves { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsQuickTest { get; private set; }
        public ShiftController Shift { get; private set; }
        private ContentCatalog content;
        private HubContent hub;
        public ContentCatalog Content => content ?? (content = GameContentLoader.Load(Resources.Load<TextAsset>("Cafe/game-content")));
        public HubContent Hub
        {
            get
            {
                if (hub == null)
                {
                    var asset = Resources.Load<TextAsset>("Cafe/hub-content");
                    if (asset == null) throw new InvalidOperationException("Missing Cafe/hub-content.json.");
                    var candidate = JsonUtility.FromJson<HubContent>(asset.text);
                    if (candidate == null) throw new InvalidOperationException("Hub content is empty.");
                    candidate.Validate(); hub = candidate;
                }
                return hub;
            }
        }
        public bool HasBackroom
        {
            get
            {
                if (Days == null) return false;
                foreach (var u in Hub.upgrades) if (u.opensBackroom && HubRules.Owns(Days.Snapshot.upgrades, u.id)) return true;
                return false;
            }
        }
        public bool TryHubAction(string action, string id, out string message)
        {
            try
            {
                RequireSession();
                CafeProgress next;
                if (action == "pack") next = HubRules.BuyPack(Days.Snapshot, Content, id);
                else if (action == "upgrade")
                {
                    var item = Array.Find(Hub.upgrades, u => u.id == id);
                    if (item == null) throw new InvalidOperationException("Không tìm thấy nâng cấp.");
                    next = HubRules.BuyUpgrade(Days.Snapshot, item);
                }
                else if (action == "talk") next = HubRules.Talk(Days.Snapshot);
                else if (action == "hire")
                {
                    var employee = Array.Find(Hub.staff ?? new StaffDefinition[0], s => s.id == id);
                    if (employee == null) throw new InvalidOperationException("Không tìm thấy nhân viên.");
                    next = HubRules.Hire(Days.Snapshot, employee);
                }
                else if (action == "quest") next = HubRules.Claim(Days.Snapshot, Hub);
                else throw new InvalidOperationException("Thao tác không hợp lệ.");
                StoreProgress(next); Days = new DayService(next); message = IsQuickTest ? "Đã cập nhật phiên thử." : "Đã lưu."; return true;
            }
            catch (Exception e) { message = e.Message; return false; }
        }
        public event Action<string> Error;
        private float saveElapsed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Saves = new SaveService(System.IO.Path.Combine(Application.persistentDataPath, "Profile"));
        }
        private void Update()
        {
            if (Days == null || IsPaused || SceneManager.GetActiveScene().name != gameplayScene
                || Days.Snapshot.phase != DayPhase.Playing) return;
            float delta = Mathf.Min(Time.deltaTime, Days.Snapshot.remainingSeconds);
            Shift?.Tick(delta);
            if (Days.Tick(delta))
            {
                Shift?.CloseOrders();
                SaveAndNavigate(afterShiftScene);
                return;
            }
            saveElapsed += Time.deltaTime;
            if (saveElapsed >= 5) { saveElapsed = 0; TrySave(); }
        }
        public void NewGame() => Execute(() =>
        {
            RequireScene(townScene);
            var progress = new CafeProgress { wallet = startingCoins };
            Saves.Save(progress);
            IsQuickTest = false;
            Days = new DayService(progress);
            Shift = null;
            Navigate(townScene);
        });
        public void LoadGame() => Execute(() =>
        {
            var progress = Saves.Load();
            string scene = progress.phase == DayPhase.Playing ? gameplayScene
                : progress.phase == DayPhase.Summary ? afterShiftScene : townScene;
            RequireScene(scene);
            var shift = progress.phase == DayPhase.Playing ? CreateShift(progress) : null;
            IsQuickTest = false;
            Days = new DayService(progress);
            Shift = shift;
            Navigate(scene);
        });
        public void StartDay() => Execute(() =>
        {
            RequireSession();
            RequireScene(gameplayScene);
            RequireScene(afterShiftScene);
            var candidate = new DayService(Days.Snapshot);
            float duration = dayDurationSeconds;
            foreach (var u in Hub.upgrades) if (HubRules.Owns(Days.Snapshot.upgrades, u.id)) duration += u.extraSeconds;
            candidate.BeginDay(duration);
            var shift = CreateShift(candidate.Snapshot);
            var initial = candidate.Snapshot;
            initial.shift = shift.Capture();
            StoreProgress(initial);
            Days = candidate;
            Shift = shift;
            Navigate(gameplayScene);
        });
        public void NextDay() => Execute(() =>
        {
            RequireSession();
            RequireScene(townScene);
            var candidate = new DayService(Days.Snapshot);
            candidate.PrepareNextDay();
            StoreProgress(candidate.Snapshot);
            Days = candidate;
            Shift = null;
            Navigate(townScene);
        });
        public void ReturnToMainMenu() => Execute(() =>
        {
            RequireScene(mainMenuScene);
            if (Days != null) StoreProgress(CaptureProgress());
            Navigate(mainMenuScene);
        });
        public void RetryEndDaySave() => Execute(() =>
        {
            RequireSession();
            if (Days.Snapshot.phase != DayPhase.Summary)
                throw new InvalidOperationException("There is no finished day to save.");
            SaveAndNavigate(afterShiftScene);
        });
        public void SetPaused(bool paused) => IsPaused = paused;

        /// <summary>Allows direct scene testing without reading or overwriting a saved game.</summary>
        public void BeginQuickTest(bool startShift)
        {
            if (Days != null) return;
            IsQuickTest = true;
            Days = new DayService(new CafeProgress { wallet = startingCoins });
            if (startShift)
            {
                Days.BeginDay(dayDurationSeconds);
                Shift = CreateShift(Days.Snapshot);
            }
        }

        private void StoreProgress(CafeProgress progress)
        {
            if (!IsQuickTest) Saves.Save(progress);
        }
        private ShiftController CreateShift(CafeProgress progress)
        {
            var catalog = Content;
            int dayNumber = progress.dayNumber;
            var supplied = new List<string>();
            foreach (var id in catalog.IngredientIds)
            {
                var ingredient = catalog.GetIngredient(id);
                if (ingredient.alwaysAvailable || (dayNumber == 1 && ingredient.freeOnFirstDay)
                    || HubRules.Owns(progress.activePacks, id))
                    supplied.Add(id);
            }
            int tips = 0;
            foreach (var u in Hub.upgrades) if (HubRules.Owns(progress.upgrades, u.id)) tips = checked(tips + u.tipBonus);
            var employee = Array.Find(Hub.staff ?? new StaffDefinition[0], s => s.id == progress.staffId);
            if (!string.IsNullOrEmpty(progress.staffId) && employee == null)
                throw new InvalidOperationException("Nhân viên trong save không có trong dữ liệu game.");
            return new ShiftController(catalog, dayNumber, supplied, () =>
                Days != null && Days.Snapshot.phase == DayPhase.Playing && !IsPaused
                && SceneManager.GetActiveScene().name == gameplayScene,
                income => Days.RecordServed(checked(income + tips)), () => Days.RecordMissed(),
                HubRules.TableCount(progress, Hub, catalog.Orders.slotCount), employee, progress.shift);
        }
        private void SaveAndNavigate(string scene) => Execute(() =>
        {
            RequireScene(scene);
            StoreProgress(CaptureProgress());
            Navigate(scene);
        });
        private void Navigate(string scene)
        {
            IsPaused = false;
            saveElapsed = 0;
            SceneManager.LoadScene(scene);
        }
        private void RequireSession()
        {
            if (Days == null) throw new InvalidOperationException("Use New Game or Load Game first.");
        }
        private static void RequireScene(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
                throw new InvalidOperationException($"Add scene '{scene}' to the Build Profile scene list.");
        }
        private CafeProgress CaptureProgress()
        {
            var progress = Days.Snapshot;
            progress.shift = progress.phase == DayPhase.Playing ? Shift?.Capture() : null;
            return progress;
        }
        private void TrySave() => Execute(() => StoreProgress(CaptureProgress()));
        private void Execute(Action action)
        {
            try { action(); }
            catch (Exception exception) { Debug.LogException(exception, this); Error?.Invoke(exception.Message); }
        }
        private void OnApplicationPause(bool paused) { if (paused && Days != null) TrySave(); }
        private void OnApplicationQuit() { if (Days != null) TrySave(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
