using BakaBakeBakery.Gameplay;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class BakeryDayCycleTests
    {
        [Test]
        public void MorningMarketTripTakesThreeSecondsBeforeOpening()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());

            Assert.That(day.BeginMarketTrip(out var cost), Is.True);
            Assert.That(cost, Is.Zero);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.TravellingToMarket));
            day.Tick(2.9f);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.TravellingToMarket));
            day.Tick(0.2f);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.ReadyToOpen));
            Assert.That(day.StartDay(), Is.True);
            Assert.That(day.RemainingSeconds, Is.EqualTo(BakeryDayCycle.DayDurationSeconds));
        }

        [Test]
        public void ProfitIncludesMorningCostAndRevenue()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());

            Assert.That(day.BeginMarketTrip(out var cost), Is.True);
            Assert.That(cost, Is.EqualTo(BakeryDayCycle.MorningBasketCost));
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();
            Assert.That(day.DailyProfit, Is.EqualTo(-BakeryDayCycle.MorningBasketCost));
            day.RecordRevenue(24);
            Assert.That(day.DailyProfit, Is.EqualTo(24 - BakeryDayCycle.MorningBasketCost));
            Assert.That(day.DailyItemsSold, Is.EqualTo(1));
        }

        [Test]
        public void MarketIsReachableWithAnEmptyCashTin()
        {
            var day = new BakeryDayCycle(new BakeryProgressData { coins = 0 });

            Assert.That(BakeryDayCycle.MorningBasketCost, Is.Zero, "Entering the market must never be a paywall.");
            Assert.That(day.BeginMarketTrip(out var cost), Is.True);
            Assert.That(cost, Is.Zero);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.TravellingToMarket));
        }

        [Test]
        public void EveryMorningAfterTheFirstIsAlsoFree()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());
            day.BeginMarketTrip(out _);
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();
            day.EndDayEarly();
            day.BeginNextMorning();

            Assert.That(day.DayNumber, Is.EqualTo(2));
            Assert.That(day.BeginMarketTrip(out var cost), Is.True);
            Assert.That(cost, Is.Zero);
        }

        [Test]
        public void EarlyCloseCreatesSummaryAndNextMorning()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());
            day.BeginMarketTrip(out _);
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();

            Assert.That(day.EndDayEarly(), Is.True);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.DaySummary));
            Assert.That(day.BeginNextMorning(), Is.True);
            Assert.That(day.DayNumber, Is.EqualTo(2));
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.MorningPreparation));
            Assert.That(day.DailyItemsSold, Is.Zero);
        }

        [Test]
        public void DailySalesSurviveSaveAndResume()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());
            day.BeginMarketTrip(out _);
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();
            day.RecordRevenue(12);
            day.RecordRevenue(12);
            var progress = new BakeryProgressData();
            day.ExportInto(progress);

            var restored = new BakeryDayCycle(progress);

            Assert.That(restored.DailyRevenue, Is.EqualTo(24));
            Assert.That(restored.DailyItemsSold, Is.EqualTo(2));
        }
    }
}
