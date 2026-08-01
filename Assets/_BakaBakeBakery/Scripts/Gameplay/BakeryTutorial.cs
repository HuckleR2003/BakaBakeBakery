using System;

namespace BakaBakeBakery.Gameplay
{
    public enum BakeryTutorialStep
    {
        Welcome = 0,
        VisitMarket = 1,
        OnTheRoad = 2,
        OpenBakery = 3,
        FirstLoaf = 4,
        DiscoverRecipe = 5,
        CloseFirstDay = 6,
        DaySummary = 7,
        Complete = 8
    }

    public enum BakeryTutorialTarget
    {
        None,
        DayBoard,
        BakerAction,
        CraftTab,
        CraftResult
    }

    public readonly struct BakeryTutorialBeat
    {
        public BakeryTutorialBeat(
            BakeryTutorialStep step,
            string title,
            string copy,
            string objective,
            string primaryReply,
            string secondaryReply,
            BakeryTutorialTarget target)
        {
            Step = step;
            Title = title;
            Copy = copy;
            Objective = objective;
            PrimaryReply = primaryReply;
            SecondaryReply = secondaryReply;
            Target = target;
        }

        public BakeryTutorialStep Step { get; }
        public string Title { get; }
        public string Copy { get; }
        public string Objective { get; }
        public string PrimaryReply { get; }
        public string SecondaryReply { get; }
        public BakeryTutorialTarget Target { get; }
        public int DisplayStage => Math.Min((int)Step + 1, (int)BakeryTutorialStep.Complete);
    }

    public static class BakeryTutorial
    {
        public const int FinalStep = (int)BakeryTutorialStep.Complete;

        public static BakeryTutorialStep Normalize(int rawStep)
        {
            return (BakeryTutorialStep)Math.Clamp(rawStep, 0, FinalStep);
        }

        public static BakeryTutorialBeat GetBeat(BakeryTutorialStep step)
        {
            return step switch
            {
                BakeryTutorialStep.Welcome => new BakeryTutorialBeat(
                    step,
                    "Zaczniemy bez pośpiechu",
                    "Hej, jestem Mila. Ta ulica potrafi pokochać małą piekarnię, ale najpierw musi poczuć, że ktoś piecze tu naprawdę dla niej. Rano dbamy o spiżarnię, potem o każdy bochenek.",
                    "Porozmawiaj z Milą i przygotuj pierwszy poranek.",
                    "Jasne. Prowadź.",
                    "Najpierw się rozejrzę",
                    BakeryTutorialTarget.None),
                BakeryTutorialStep.VisitMarket => new BakeryTutorialBeat(
                    step,
                    "Najpierw pełny koszyk",
                    "Mąka kupiona w panice zawsze kosztuje więcej nerwów niż monet. Bierzemy rower, jedziemy na targ i wracamy z zapasem na całą zmianę. Pierwszy koszyk stawiam ja.",
                    "Kliknij drewnianą tabliczkę i jedź na poranny targ.",
                    "Jedziemy na targ",
                    "Daj mi chwilkę",
                    BakeryTutorialTarget.DayBoard),
                BakeryTutorialStep.OnTheRoad => new BakeryTutorialBeat(
                    step,
                    "Krótka droga do miasta",
                    "Trzy zakręty, stary mostek i jesteśmy. Z czasem rower zamienimy na samochód, ale tej trasy będziesz jeszcze trochę słuchać o poranku.",
                    "Poczekaj, aż rower wróci pod piekarnię.",
                    "Lubię tę trasę",
                    "Spotkajmy się na miejscu",
                    BakeryTutorialTarget.None),
                BakeryTutorialStep.OpenBakery => new BakeryTutorialBeat(
                    step,
                    "Koszyk pełny. Roleta w górę?",
                    "Mamy mąkę, mleko, ciasto francuskie, dżem i czekoladę. Odwróć tabliczkę dopiero, kiedy jesteś gotowy — od tej chwili sąsiedzi liczą czas razem z nami.",
                    "Rozpocznij pierwszy pięciominutowy dzień.",
                    "Otwieramy!",
                    "Jeszcze sprawdzę ladę",
                    BakeryTutorialTarget.DayBoard),
                BakeryTutorialStep.FirstLoaf => new BakeryTutorialBeat(
                    step,
                    "Nic nie pojawia się znikąd",
                    "Klikaj Julesa, kiedy czeka na decyzję. Najpierw pójdzie po składniki, potem przygotuje surowe ciasto, wsunie je do pieca i dopiero gotowy bochenek zaniesie na pustą ladę.",
                    "Pomóż Julesowi upiec i wystawić pierwszy chleb.",
                    "Jules, zaczynamy",
                    "Sam kliknę go za moment",
                    BakeryTutorialTarget.BakerAction),
                BakeryTutorialStep.DiscoverRecipe => new BakeryTutorialBeat(
                    step,
                    "Zostawmy po sobie własny przepis",
                    "Ten bochenek już pachnie jak dom. A teraz mały eksperyment: przeciągnij do misek mąkę, mleko i czekoladę. Nieudane pomysły nic nie kosztują — składniki znikają dopiero po prawdziwym odkryciu.",
                    "Otwórz Pracownię i odkryj czekoladową babeczkę.",
                    "Pokaż mi Pracownię",
                    "Najpierw nacieszę się chlebem",
                    BakeryTutorialTarget.CraftTab),
                BakeryTutorialStep.CloseFirstDay => new BakeryTutorialBeat(
                    step,
                    "Masz oko, piekarzu",
                    "Kurde, no i masz oko! Ale dziś nie rozpędzajmy się za bardzo. Ujmijmy sąsiedztwo ciepłym, dobrym chlebem, a jutro zaskoczymy ich babeczką.",
                    "Sprzedaj pierwszy chleb i zakończ zmianę tabliczką.",
                    "Dziś liczy się chleb",
                    "Zostaw mi cel na ekranie",
                    BakeryTutorialTarget.DayBoard),
                BakeryTutorialStep.DaySummary => new BakeryTutorialBeat(
                    step,
                    "Pierwsza roleta opuszczona",
                    "Przychód mówi, ile wpadło do kasetki. Koszty przypominają, ile zostawiliśmy rano na targu. Dopiero różnica jest prawdziwym zyskiem piekarni.",
                    "Przejrzyj wynik i rozpocznij następny poranek.",
                    "Jutro ich zaskoczymy",
                    "Jeszcze popatrzę na wynik",
                    BakeryTutorialTarget.DayBoard),
                _ => new BakeryTutorialBeat(
                    BakeryTutorialStep.Complete,
                    "Ta piekarnia jest już Twoja",
                    "Umiesz kupić zapasy, poprowadzić Julesa, zamknąć dzień i odkrywać własne wypieki. Ja zostaję obok — po dziesięciu chlebach przejmę rytm pracy jako managerka.",
                    "Piecz, eksperymentuj i ogrzewaj sąsiedztwo po swojemu.",
                    "Wracam do piekarni",
                    "Dzięki, Mila",
                    BakeryTutorialTarget.None)
            };
        }
    }
}
