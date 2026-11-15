using Application.Common.Periods;
using Domain.Enums;
using Application.Features.ImportFeatures;

namespace Application.Common.DTOs;

public class ImportReportDto
{
    public ImportMode Mode { get; set; }
    public string PreviewToken { get; set; } = "";
    public bool CanSynchronize { get; set; }
    public string ScopeDescription { get; set; } = "";
    public List<ImportChangeDto> Changes { get; set; } = [];
    public List<string> NewTeachers { get; set; } = [];
    public List<string> NewDisciplines { get; set; } = [];
    public ImportPeriodDto Period { get; set; } = null!;
    public ImportSummaryDto Summary { get; set; } = null!;
    public List<ImportIssueDto> Errors { get; set; } = [];
    public List<ImportIssueDto> Warnings { get; set; } = [];
    public List<ImportWorkloadDto> WorkloadsToDelete { get; set; } = [];

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
    public int Update { get; set; }
    public int Existing { get; set; }
    public int Delete { get; set; }
}

public record ImportWorkloadDto(string Teacher, string Discipline, string Group);

public record ImportIssueDto(string Sheet, int Row, string Message);
public record ImportChangeDto(string Entity, string Key, string Field, string Before, string After);

public class StudentCredentialDto
{
    public string GroupId { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Login { get; set; } = null!;
    public bool MustChangePassword { get; set; }
}
