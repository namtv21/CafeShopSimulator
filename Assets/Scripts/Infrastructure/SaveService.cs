using System.IO;
using UnityEngine;

namespace CafeShopSimulator
{
    public sealed class SaveService
    {
        public string SavePath { get; }
        public bool HasSave => File.Exists(SavePath);

        public SaveService(string folder)
        {
            SavePath = Path.Combine(folder, "cafe-progress-v1.json");
        }

        public CafeProgress Load()
        {
            var state = JsonUtility.FromJson<CafeProgress>(File.ReadAllText(SavePath));
            if (state == null) throw new InvalidDataException("Save file is empty.");
            // JsonUtility serializes inline reference types by value, so null alone is not
            // an absence marker. Explicit presence flags also distinguish legacy saves.
            if (state.phase != DayPhase.Playing || !state.hasShiftSnapshot) state.shift = null;
            else if (state.shift == null) throw new InvalidDataException("Missing shift snapshot.");
            if (state.shift != null && !state.shift.hasStaff) state.shift.staff = null;
            state.Validate();
            return state;
        }

        public void Save(CafeProgress state)
        {
            state = state.Copy();
            state.hasShiftSnapshot = state.phase == DayPhase.Playing && state.shift != null;
            if (!state.hasShiftSnapshot) state.shift = null;
            state.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));
            if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, SavePath + ".bak");
            else File.Move(temporaryPath, SavePath);
        }
    }
}
