using Application.Common.CQRS;
using Application.Common.Exceptions;
using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using Domain.Enums;
using MediatR;

namespace Application.Features.ImportFeatures;

/// <summary>
/// Выгрузка нагрузки семестра в формате файла импорта: файл можно поправить и загрузить обратно.
/// На лист «Студенты» попадают студенты групп из выгруженной нагрузки, на скрытый лист — срез выгрузки.
/// Без teacherIds — вся нагрузка семестра.
/// </summary>
public record ExportPeriodQuery(int StartYear, Term Term, IReadOnlyCollection<string>? TeacherIds) : IRequest<byte[]>, IQuery;

public class ExportPeriodQueryHandler : IRequestHandler<ExportPeriodQuery, byte[]>
{
    private readonly IImportRepository _importRepository;
    private readonly IRosterFileWriter _writer;

    public ExportPeriodQueryHandler(IImportRepository importRepository, IRosterFileWriter writer)
    {
        _importRepository = importRepository;
        _writer = writer;
    }

    public async Task<byte[]> Handle(ExportPeriodQuery request, CancellationToken cancellationToken)
    {
        var period = new PeriodKey(request.StartYear, request.Term);
        var teacherIds = request.TeacherIds is { Count: > 0 } ids ? ids : null;

        var export = await _importRepository.GetPeriodExportAsync(period, teacherIds, cancellationToken)
            ?? throw new NotFoundException($"Семестр «{PeriodCalculator.Title(period.StartYear, period.Term)}» не найден");

        var workloads = export.Workloads
            .OrderBy(w => w.TeacherFio)
            .ThenBy(w => w.Discipline)
            .ThenBy(w => w.Group)
            .Select(w => (IReadOnlyList<string>)[w.TeacherFio, w.Discipline, w.Group, w.TeacherId, w.DisciplineId])
            .ToList();

        var students = export.Students
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Surname)
            .ThenBy(s => s.Name)
            .Select(s => (IReadOnlyList<string>)[s.Group, s.Surname, s.Name, s.Patronymic ?? "", s.StudentNumber])
            .ToList();

        List<IReadOnlyList<string>> scope = [[ImportFormat.ScopePeriod, ImportFormat.PeriodValue(period)]];
        scope.AddRange((teacherIds ?? []).Select(id => (IReadOnlyList<string>)[ImportFormat.ScopeTeacher, id]));
        scope.Add([ImportFormat.ScopeTeacherCount, (teacherIds?.Distinct().Count() ?? 0).ToString()]);

        return _writer.Write([
            new SpreadsheetSheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns, students),
            new SpreadsheetSheet(ImportFormat.WorkloadsSheet, ImportFormat.ExportWorkloadColumns, workloads),
            new SpreadsheetSheet(ImportFormat.ScopeSheet, ImportFormat.ScopeColumns, scope, Hidden: true),
        ]);
    }
}
