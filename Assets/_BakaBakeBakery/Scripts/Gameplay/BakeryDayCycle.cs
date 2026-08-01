using System;

namespace BakaBakeBakery.Gameplay
{
    public enum BakeryDayPhase
    {
        MorningPreparation,
        TravellingToMarket,
        ReadyToOpen,
        Open,
        DaySummary
    }

    public sealed class BakeryDayCycle
    {
        public const float MarketTripSeconds = 3f;
        public const float DayDurationSeconds = 300f;
        public const int MorningBasketCost = 18;

        public BakeryDayCycle(BakeryProgressData progress)
        {
            var safe = BakeryProgressStore.Sanitize(progress);
            DayNumber = safe.dayNumber;
            Phase = Enum.IsDefined(typeof(BakeryDayPhase), safe.dayPhase)
                ? (BakeryDayPhase)safe.dayPhase
                : BakeryDayPhase.MorningPreparation;
            RemainingSeconds = Phase == BakeryDayPhase.Open
                ? Math.Clamp(safe.daySecondsRemaining, 1f, DayDurationSeconds)
                : 0f;
            DailyCosts = Math.Max(0, safe.dailyCosts);
            DailyRevenue = Math.Max(0, safe.dailyRevenue);
        }

        public int DayNumber { get; private set; }
        public BakeryDayPhase Phase { get; private set; }
        public float RemainingSeconds { get; private set; }
        public int DailyCosts { get; private set; }
        public int DailyRevenue { get; private set; }
        public int DailyProfit => DailyRevenue - DailyCosts;
        public float TravelProgress => Phase == BakeryDayPhase.TravellingToMarket
            ? 1f - RemainingSeconds / MarketTripSeconds
            : Phase > BakeryDayPhase.TravellingToMarket ? 1f : 0f;

        public bool BeginMarketTrip(bool canAfford, bool tutorialBasket, out int cost)
        {
            cost = tutorialBasket ? 0 : MorningBasketCost;
            if (Phase != BakeryDayPhase.MorningPreparation || (!tutorialBasket && !canAfford))
            {
                return false;
            }

            DailyCosts = cost;
            DailyRevenue = 0;
            RemainingSeconds = MarketTripSeconds;
            Phase = BakeryDayPhase.TravellingToMarket;
            return true;
        }

        public bool StartDay()
        {
            if (Phase != BakeryDayPhase.ReadyToOpen)
            {
                return false;
            }

            RemainingSeconds = DayDurationSeconds;
            Phase = BakeryDayPhase.Open;
            return true;
        }

        public bool EndDayEarly()
        {
            if (Phase != BakeryDayPhase.Open)
            {
                return false;
            }

            RemainingSeconds = 0f;
            Phase = BakeryDayPhase.DaySummary;
            return true;
        }

        public bool BeginNextMorning()
        {
            if (Phase != BakeryDayPhase.DaySummary)
            {
                return false;
            }

            DayNumber = Math.Min(9999, DayNumber + 1);
            DailyCosts = 0;
            DailyRevenue = 0;
            Phase = BakeryDayPhase.MorningPreparation;
            return true;
        }

        public void RecordRevenue(int amount)
        {
            if (Phase == BakeryDayPhase.Open && amount > 0)
            {
                DailyRevenue = Math.Min(1000000000, DailyRevenue + amount);
            }
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            {
                return;
            }

            if (Phase != BakeryDayPhase.TravellingToMarket && Phase != BakeryDayPhase.Open)
            {
                return;
            }

            RemainingSeconds = Math.Max(0f, RemainingSeconds - Math.Min(deltaTime, 5f));
            if (RemainingSeconds > 0f)
            {
                return;
            }

            Phase = Phase == BakeryDayPhase.TravellingToMarket
                ? BakeryDayPhase.ReadyToOpen
                : BakeryDayPhase.DaySummary;
        }

        public void ForceOpenForTesting()
        {
            Phase = BakeryDayPhase.Open;
            RemainingSeconds = DayDurationSeconds;
        }

        public void ExportInto(BakeryProgressData progress)
        {
            progress.dayNumber = DayNumber;
            progress.dayPhase = (int)Phase;
            progress.daySecondsRemaining = RemainingSeconds;
            progress.dailyCosts = DailyCosts;
            progress.dailyRevenue = DailyRevenue;
        }
    }
}
