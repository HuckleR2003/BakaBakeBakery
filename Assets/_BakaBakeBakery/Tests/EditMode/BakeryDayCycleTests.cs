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

            Assert.That(day.BeginMarketTrip(false, true, out var cost), Is.True);
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

            Assert.That(day.BeginMarketTrip(true, false, out var cost), Is.True);
            Assert.That(cost, Is.EqualTo(BakeryDayCycle.MorningBasketCost));
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();
            Assert.That(day.DailyProfit, Is.EqualTo(-BakeryDayCycle.MorningBasketCost));
            day.RecordRevenue(24);
            Assert.That(day.DailyProfit, Is.EqualTo(6));
        }

        [Test]
        public void EarlyCloseCreatesSummaryAndNextMorning()
        {
            var day = new BakeryDayCycle(new BakeryProgressData());
            day.BeginMarketTrip(false, true, out _);
            day.Tick(BakeryDayCycle.MarketTripSeconds);
            day.StartDay();

            Assert.That(day.EndDayEarly(), Is.True);
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.DaySummary));
            Assert.That(day.BeginNextMorning(), Is.True);
            Assert.That(day.DayNumber, Is.EqualTo(2));
            Assert.That(day.Phase, Is.EqualTo(BakeryDayPhase.MorningPreparation));
        }
    }
}
