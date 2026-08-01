using System;
using System.Collections.Generic;
using BakaBakeBakery.Data;
using BakaBakeBakery.Gameplay;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    /// <summary>
    /// Long-session stability. These tests replay the exact update order the game controller uses
    /// (day cycle, then production, then milestone spending) for a quarter of an hour of simulated
    /// play, so a stall, a runaway counter or a dead economy fails here instead of in a play test.
    /// </summary>
    public sealed class BakerySoakTests
    {
        private const float FrameSeconds = 1f / 60f;
        private const float TargetSessionSeconds = 15f * 60f;
        private const float StallToleranceSeconds = 45f;

        [Test]
        public void AQuarterOfAnHourOfPlayNeverStallsAndKeepsGrowing()
        {
            var session = Play(TargetSessionSeconds);

            Assert.That(session.LongestSilenceWhileOpen, Is.LessThan(StallToleranceSeconds),
                $"Production went quiet for {session.LongestSilenceWhileOpen:0.0}s while the bakery was open.");
            Assert.That(session.DayNumber, Is.GreaterThanOrEqualTo(3),
                "Fifteen minutes should cover at least three trading days.");
            Assert.That(session.TotalItemsSold, Is.GreaterThanOrEqualTo(BakeryLoop.BakeryUpgradeSales),
                "The wooden bakery milestone must be reachable inside a first sitting.");
            Assert.That(session.ManagerUnlockedAtSeconds, Is.GreaterThan(0f).And.LessThan(300f),
                "Mila should take over during the first shift.");
            Assert.That(session.SecondOvenPurchased, Is.True, "The second oven must be affordable in fifteen minutes.");
            Assert.That(session.BakeryLevel, Is.EqualTo(2), "The wooden bakery must be affordable in fifteen minutes.");
        }

        [Test]
        public void LongSessionInvariantsHoldEveryFrame()
        {
            var session = Play(TargetSessionSeconds);

            Assert.That(session.CounterOverflowFrames, Is.Zero, "Counter stock exceeded its capacity.");
            Assert.That(session.NegativeCoinFrames, Is.Zero, "The cash tin went negative.");
            Assert.That(session.InvalidNumberFrames, Is.Zero, "A timer produced NaN or infinity.");
            Assert.That(session.PhaseRegressionFrames, Is.Zero, "The day cycle moved backwards through its phases.");
            Assert.That(session.Coins, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ClickSpamAndBrokenFrameTimesCannotDerailALongSession()
        {
            var session = Play(TargetSessionSeconds, spamInput: true, injectBrokenDeltas: true);

            Assert.That(session.CounterOverflowFrames, Is.Zero);
            Assert.That(session.NegativeCoinFrames, Is.Zero);
            Assert.That(session.InvalidNumberFrames, Is.Zero);
            Assert.That(session.TotalItemsSold, Is.GreaterThan(0));
            Assert.That(session.LongestSilenceWhileOpen, Is.LessThan(StallToleranceSeconds));
        }

        [Test]
        public void EmptyPurseStillReachesTheMarketEveryMorning()
        {
            var session = Play(TargetSessionSeconds, startingCoins: 0);

            Assert.That(session.MarketTripsTaken, Is.GreaterThanOrEqualTo(3),
                "A morning market run must never be blocked by an empty cash tin.");
            Assert.That(session.TotalItemsSold, Is.GreaterThan(0));
        }

        private static SessionReport Play(
            float seconds,
            bool spamInput = false,
            bool injectBrokenDeltas = false,
            int? startingCoins = null)
        {
            var progress = BakeryProgressStore.CreateNewGame();
            if (startingCoins.HasValue)
            {
                progress.coins = startingCoins.Value;
            }

            var loop = new BakeryLoop(Catalog(), progress);
            var day = new BakeryDayCycle(progress);
            var report = new SessionReport();
            var elapsed = 0f;
            var silence = 0f;
            var lastPhase = day.Phase;
            var spamCounter = 0;

            while (elapsed < seconds)
            {
                if (injectBrokenDeltas && (int)(elapsed * 60f) % 601 == 0)
                {
                    day.Tick(float.NaN);
                    loop.Tick(float.PositiveInfinity);
                    loop.Tick(-4f);
                }

                day.Tick(FrameSeconds);

                // Sampled before the player acts, so the natural end-of-day rollover is not mistaken
                // for the clock itself running backwards.
                var phaseAfterTick = day.Phase;
                if (day.Phase == BakeryDayPhase.Open)
                {
                    loop.Tick(FrameSeconds);
                }

                DrainEvents(loop, day, report);
                AdvanceDay(loop, day, report);

                if (day.Phase == BakeryDayPhase.Open)
                {
                    loop.RequestAction();
                    if (spamInput && ++spamCounter % 3 == 0)
                    {
                        loop.RequestAction();
                        loop.RequestAction();
                        loop.TrySelectRecipe(RecipeId.KaiserRoll);
                    }
                }

                BuyWhatIsAffordable(loop);
                Inspect(loop, day, phaseAfterTick, report, ref lastPhase);

                if (day.Phase == BakeryDayPhase.Open)
                {
                    silence += FrameSeconds;
                    if (report.ConsumeSaleFlag())
                    {
                        silence = 0f;
                    }

                    report.LongestSilenceWhileOpen = Math.Max(report.LongestSilenceWhileOpen, silence);
                }
                else
                {
                    silence = 0f;
                    report.ConsumeSaleFlag();
                }

                elapsed += FrameSeconds;
                report.ElapsedSeconds = elapsed;
            }

            report.Capture(loop, day);
            return report;
        }

        private static void AdvanceDay(BakeryLoop loop, BakeryDayCycle day, SessionReport report)
        {
            switch (day.Phase)
            {
                case BakeryDayPhase.MorningPreparation:
                    if (day.BeginMarketTrip(out var cost))
                    {
                        report.MarketTripsTaken++;
                        if (cost > 0)
                        {
                            loop.TrySpendCoins(cost);
                        }
                    }

                    break;
                case BakeryDayPhase.ReadyToOpen:
                    day.StartDay();
                    break;
                case BakeryDayPhase.DaySummary:
                    loop.CloseShift();
                    day.BeginNextMorning();
                    break;
            }
        }

        private static void BuyWhatIsAffordable(BakeryLoop loop)
        {
            loop.TryPurchaseSecondOven();
            loop.TryPurchaseBakeryUpgrade();
        }

        private static void DrainEvents(BakeryLoop loop, BakeryDayCycle day, SessionReport report)
        {
            while (loop.TryDequeueEvent(out var loopEvent))
            {
                switch (loopEvent.Type)
                {
                    case BakeryLoopEventType.SaleCompleted:
                        day.RecordRevenue(loopEvent.Amount);
                        report.MarkSale();
                        break;
                    case BakeryLoopEventType.ManagerUnlocked:
                        if (report.ManagerUnlockedAtSeconds <= 0f)
                        {
                            report.ManagerUnlockedAtSeconds = Math.Max(0.001f, report.ElapsedSeconds);
                        }

                        break;
                }
            }
        }

        private static void Inspect(
            BakeryLoop loop,
            BakeryDayCycle day,
            BakeryDayPhase phaseAfterTick,
            SessionReport report,
            ref BakeryDayPhase lastPhase)
        {
            var snapshot = loop.Snapshot;
            if (snapshot.CounterStock > snapshot.CounterCapacity)
            {
                report.CounterOverflowFrames++;
            }

            if (snapshot.Coins < 0)
            {
                report.NegativeCoinFrames++;
            }

            if (IsBroken(snapshot.PhaseRemaining)
                || IsBroken(snapshot.PhaseProgress)
                || IsBroken(snapshot.GoldenMinuteRemaining)
                || IsBroken(day.RemainingSeconds))
            {
                report.InvalidNumberFrames++;
            }

            var wrappedToNextMorning = lastPhase == BakeryDayPhase.DaySummary
                && phaseAfterTick == BakeryDayPhase.MorningPreparation;
            if (phaseAfterTick < lastPhase && !wrappedToNextMorning)
            {
                report.PhaseRegressionFrames++;
            }

            lastPhase = phaseAfterTick;
        }

        private static bool IsBroken(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f;
        }

        private static IEnumerable<BakeryRecipeSpec> Catalog()
        {
            return new List<BakeryRecipeSpec>
            {
                new(RecipeId.CountryBread, "Country Bread", 4f, 1, 6, 0, 1),
                new(RecipeId.KaiserRoll, "Basic Kaiser Roll", 6f, 3, 3, 30, 1),
                new(RecipeId.ButterCroissant, "Butter Croissant", 8f, 2, 8, 45, 1),
                new(RecipeId.CinnamonSwirl, "Cinnamon Swirl", 9f, 3, 7, 75, 2),
                new(RecipeId.Finezja, "Finezja", 14f, 2, 11, 100, 2),
                new(RecipeId.CinnamonMonocle, "Cinnamon Monocle", 10f, 3, 9, 125, 2)
            };
        }

        private sealed class SessionReport
        {
            private bool saleThisFrame;

            public float ElapsedSeconds { get; set; }
            public float LongestSilenceWhileOpen { get; set; }
            public float ManagerUnlockedAtSeconds { get; set; }
            public int MarketTripsTaken { get; set; }
            public int CounterOverflowFrames { get; set; }
            public int NegativeCoinFrames { get; set; }
            public int InvalidNumberFrames { get; set; }
            public int PhaseRegressionFrames { get; set; }
            public int TotalItemsSold { get; private set; }
            public int Coins { get; private set; }
            public int BakeryLevel { get; private set; }
            public int DayNumber { get; private set; }
            public bool SecondOvenPurchased { get; private set; }

            public void MarkSale() => saleThisFrame = true;

            public bool ConsumeSaleFlag()
            {
                var value = saleThisFrame;
                saleThisFrame = false;
                return value;
            }

            public void Capture(BakeryLoop loop, BakeryDayCycle day)
            {
                var snapshot = loop.Snapshot;
                TotalItemsSold = snapshot.TotalItemsSold;
                Coins = snapshot.Coins;
                BakeryLevel = snapshot.BakeryLevel;
                SecondOvenPurchased = snapshot.SecondOvenPurchased;
                DayNumber = day.DayNumber;
            }
        }
    }
}
