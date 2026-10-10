using Application.Common.Periods;
using Domain.Enums;

namespace Application.UnitTests;

public class PeriodCalculatorTests
{
    [Theory]
    [InlineData(2027, 1, 31, 2026, Term.Autumn)]
    [InlineData(2027, 2, 1, 2026, Term.Spring)]
    [InlineData(2027, 8, 31, 2026, Term.Spring)]
    [InlineData(2026, 9, 1, 2026, Term.Autumn)]
    [InlineData(2026, 12, 31, 2026, Term.Autumn)]
    public void Current_ReturnsPeriodByMonth(int year, int month, int day, int startYear, Term term)
    {
        var period = PeriodCalculator.Current(new DateOnly(year, month, day));

        Assert.Equal(new PeriodKey(startYear, term), period);
    }

    [Fact]
    public void PreviousAndNext_CrossAcademicYear()
    {
        var autumn = new PeriodKey(2026, Term.Autumn);

        Assert.Equal(new PeriodKey(2025, Term.Spring), PeriodCalculator.Previous(autumn));
        Assert.Equal(new PeriodKey(2026, Term.Spring), PeriodCalculator.Next(autumn));
        Assert.Equal(new PeriodKey(2027, Term.Autumn), PeriodCalculator.Next(new PeriodKey(2026, Term.Spring)));
    }

    [Theory]
    [InlineData(2026, Term.Autumn, "2026/27, осенний семестр")]
    [InlineData(2026, Term.Spring, "2026/27, весенний семестр")]
    [InlineData(2099, Term.Autumn, "2099/00, осенний семестр")]
    public void Title_FormatsAcademicYear(int startYear, Term term, string expected)
    {
        Assert.Equal(expected, PeriodCalculator.Title(startYear, term));
    }
}
