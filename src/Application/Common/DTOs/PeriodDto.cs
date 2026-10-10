using Application.Common.Periods;
using Domain.Enums;

namespace Application.Common.DTOs;

public class PeriodDto
{
    public string Id { get; set; } = null!;
    public int StartYear { get; set; }
    public Term Term { get; set; }
    public string Title => PeriodCalculator.Title(StartYear, Term);
    public bool IsFeedbackOpen { get; set; }
}

public class PeriodListItemDto : PeriodDto
{
    public int WorkloadCount { get; set; }
    public int FeedbackCount { get; set; }
}

public class SuggestedPeriodDto
{
    public string? Id { get; set; }
    public int StartYear { get; set; }
    public Term Term { get; set; }
    public string Title => PeriodCalculator.Title(StartYear, Term);
    public bool Exists { get; set; }
    public bool IsCurrent { get; set; }
}
