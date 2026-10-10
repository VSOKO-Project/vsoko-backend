using Application.Common.Periods;
using Domain.Enums;

namespace Application.Common.DTOs;

public class ImportReportDto
{
    public ImportPeriodDto Period { get; set; } = null!;
    public ImportSummaryDto Summary { get; set; } = null!;
    public List<ImportIssueDto> Errors { get; set; } = [];
    public List<ImportIssueDto> Warnings { get; set; } = [];

    // Заполняются только в ответе apply.
    public List<string>? CreatedGroupIds { get; set; }
    public List<string>? StudentGroupIds { get; set; }
}

public class ImportPeriodDto
{
    public string? Id { get; set; }
    public int StartYear { get; set; }
    public Term Term { get; set; }
    public string Title => PeriodCalculator.Title(StartYear, Term);
    public bool WillBeCreated { get; set; }
}

public class ImportSummaryDto
{
    public ImportCountDto Groups { get; set; } = new();
    public ImportCountDto Students { get; set; } = new();
    public ImportCountDto Teachers { get; set; } = new();
    public ImportCountDto Disciplines { get; set; } = new();
    public ImportCountDto Workloads { get; set; } = new();
}

public class ImportCountDto
{
    public int Create { get; set; }
    public int Existing { get; set; }
}

public record ImportIssueDto(string Sheet, int Row, string Message);

public class StudentCredentialDto
{
    public string GroupId { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Login { get; set; } = null!;
    public bool MustChangePassword { get; set; }
}
