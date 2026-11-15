namespace Application.Common.DTOs;

public record DashboardBucketDto(string Label, int Value);
public record DashboardCoverageDto(long SubmittedCount, long PossibleCount, double? CompletionPercent);
public record DashboardComparisonDto(string CurrentTitle, string PreviousTitle, double Delta, int CurrentFeedbackCount, int PreviousFeedbackCount);
public record DashboardSemesterDto(string PeriodId, string Title, int FeedbackCount, double? AverageGrade, int DisciplineCount, DashboardCoverageDto Coverage);
public record DashboardDto(
    int FeedbackCount,
    int TeacherCount,
    int DisciplineCount,
    double? AverageGrade,
    int LowGradeCount,
    List<DashboardBucketDto> Distribution,
    List<RatingDto> LowGrades,
    List<RatingDto> HighGrades,
    List<DashboardSemesterDto> Timeline,
    DashboardCoverageDto Coverage,
    int RatedFeedbackCount,
    DashboardComparisonDto? Comparison);
