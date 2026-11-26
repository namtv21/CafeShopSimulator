using System;

namespace CafeShopSimulator
{
    /// <summary>Pure C# rules for one day. Does not know about scenes, UI or files.</summary>
    public sealed class DayService
    {
        private CafeProgress state;
        public CafeProgress Snapshot => state.Copy();
        public event Action Changed;

        public DayService(CafeProgress progress)
        {
            progress.Validate();
            state = progress.Copy();
        }

        public void BeginDay(float durationSeconds)
        {
            if (state.phase != DayPhase.Preparation)
                throw new InvalidOperationException("Prepare the day before opening the cafe.");
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            state.remainingSeconds = durationSeconds;
            state.activePacks = state.pendingPacks;
            state.pendingPacks = new string[0];
            state.runIncome = state.servedCustomers = state.missedCustomers = 0;
            state.phase = DayPhase.Playing;
            state.shift = null;
            Changed?.Invoke();
        }

        public bool Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (state.phase != DayPhase.Playing) return false;
            state.remainingSeconds = Math.Max(0, state.remainingSeconds - deltaSeconds);
            return state.remainingSeconds <= 0 && FinishDay();
        }

        public void RecordServed(int income)
        {
            RequirePlaying();
            if (income < 0) throw new ArgumentOutOfRangeException(nameof(income));
            int total = checked(state.runIncome + income);
            if ((long)state.wallet + total > int.MaxValue)
                throw new OverflowException("Wallet capacity exceeded.");
            int served = checked(state.servedCustomers + 1);
            state.runIncome = total;
            state.servedCustomers = served;
            Changed?.Invoke();
        }

        public void RecordMissed()
        {
            RequirePlaying();
            state.missedCustomers = checked(state.missedCustomers + 1);
            Changed?.Invoke();
        }

        public bool FinishDay()
        {
            if (state.phase != DayPhase.Playing) return false;
            int wallet = checked(state.wallet + state.runIncome);
            int total = checked(state.totalServed + state.servedCustomers);
            state.wallet = wallet;
            state.totalServed = total;
            state.remainingSeconds = 0;
            state.phase = DayPhase.Summary;
            state.shift = null;
            Changed?.Invoke();
            return true;
        }

        public void PrepareNextDay()
        {
            if (state.phase != DayPhase.Summary)
                throw new InvalidOperationException("Finish the current day first.");
            state.dayNumber = checked(state.dayNumber + 1);
            state.phase = DayPhase.Preparation;
            state.shift = null;
            state.runIncome = state.servedCustomers = state.missedCustomers = 0;
            Changed?.Invoke();
        }

        private void RequirePlaying()
        {
            if (state.phase != DayPhase.Playing)
                throw new InvalidOperationException("The cafe is not open.");
        }
    }
}
