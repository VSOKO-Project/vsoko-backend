using Domain.Enums;

namespace Application.Common.Periods;

public readonly record struct PeriodKey(int StartYear, Term Term);

public static class PeriodCalculator
{
    // Сентябрь–декабрь — осень текущего года, январь — осень прошлого, февраль–август — весна прошлого.
    public static PeriodKey Current(DateOnly today) =>
        today.Month switch
        {
            >= 9 => new PeriodKey(today.Year, Term.Autumn),
            1 => new PeriodKey(today.Year - 1, Term.Autumn),
            _ => new PeriodKey(today.Year - 1, Term.Spring),
        };

    public static PeriodKey Previous(PeriodKey period) =>
        period.Term == Term.Autumn
            ? new PeriodKey(period.StartYear - 1, Term.Spring)
            : new PeriodKey(period.StartYear, Term.Autumn);

    public static PeriodKey Next(PeriodKey period) =>
        period.Term == Term.Autumn
            ? new PeriodKey(period.StartYear, Term.Spring)
            : new PeriodKey(period.StartYear + 1, Term.Autumn);

    public static string YearTitle(int startYear) => $"{startYear}/{(startYear + 1) % 100:D2}";

    public static string Title(int startYear, Term term) =>
        $"{YearTitle(startYear)}, {(term == Term.Autumn ? "осенний" : "весенний")} семестр";
}
