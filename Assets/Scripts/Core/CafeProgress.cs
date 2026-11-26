using System;

namespace CafeShopSimulator
{
    public enum DayPhase { Preparation, Playing, Summary }

    [Serializable]
    public sealed class CafeProgress
    {
        public int version = 1;
        public int dayNumber = 1;
        public int wallet;
        public DayPhase phase = DayPhase.Preparation;
        public float remainingSeconds;
        public int runIncome;
        public int servedCustomers;
        public int missedCustomers;
        public string[] pendingPacks = new string[0];
        public string[] activePacks = new string[0];
        public string[] upgrades = new string[0];
        public int affection;
        public int lastChatDay;
        public int lastQuestDay;
        public int totalServed;
        public string staffId;
        public bool hasShiftSnapshot;
        public ShiftSave shift;

        public CafeProgress Copy()
        {
            var copy = (CafeProgress)MemberwiseClone();
            copy.pendingPacks = (string[])(pendingPacks ?? new string[0]).Clone();
            copy.activePacks = (string[])(activePacks ?? new string[0]).Clone();
            copy.upgrades = (string[])(upgrades ?? new string[0]).Clone();
            copy.shift = shift?.Copy();
            return copy;
        }

        public void Validate()
        {
            if (version != 1 || dayNumber < 1 || wallet < 0 || runIncome < 0
                || servedCustomers < 0 || missedCustomers < 0 || affection < 0 || totalServed < 0
                || lastChatDay < 0 || lastChatDay > dayNumber || lastQuestDay < 0 || lastQuestDay > dayNumber
                || !Enum.IsDefined(typeof(DayPhase), phase)
                || float.IsNaN(remainingSeconds) || float.IsInfinity(remainingSeconds)
                || remainingSeconds < 0
                || (phase == DayPhase.Playing && (long)wallet + runIncome > int.MaxValue))
                throw new InvalidOperationException("Save data is invalid or unsupported.");
        }
    }

    [Serializable]
    public sealed class ShiftSave
    {
        public string[] cup;
        public OrderSave[] orders;
        public StaffSave staff;
        public bool hasStaff;
        public ShiftSave Copy()
        {
            var copy = (ShiftSave)MemberwiseClone();
            copy.cup = cup == null ? null : (string[])cup.Clone();
            if (orders != null)
            {
                copy.orders = new OrderSave[orders.Length];
                for (int i = 0; i < orders.Length; i++) copy.orders[i] = orders[i]?.Copy();
            }
            copy.staff = staff?.Copy();
            return copy;
        }
    }

    [Serializable]
    public sealed class OrderSave
    {
        public long id;
        public string recipeId;
        public float patience, arrival, walkRemaining;
        public bool closed;
        public OrderSave Copy() => (OrderSave)MemberwiseClone();
    }
}
