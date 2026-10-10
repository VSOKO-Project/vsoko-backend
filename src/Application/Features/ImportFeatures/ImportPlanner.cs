using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;

namespace Application.Features.ImportFeatures;

/// <summary>
/// Общий для preview и apply шаг: прочитать файл, загрузить справочники, построить план.
/// </summary>
public class ImportPlanner
{
    private readonly IRosterFileReader _reader;
    private readonly IImportRepository _importRepository;

    public ImportPlanner(IRosterFileReader reader, IImportRepository importRepository)
    {
        _reader = reader;
        _importRepository = importRepository;
    }

    public async Task<(ImportPlan Plan, ImportReferenceData Refs)> PlanAsync(
        ImportFileRequest request,
        CancellationToken cancellationToken
    )
    {
        var sheets = _reader.Read(request.Content!);
        var period = new PeriodKey(request.StartYear, request.Term);
        var refs = await _importRepository.LoadReferenceDataAsync(period, cancellationToken);

        return (ImportPlanBuilder.Build(sheets, period, refs), refs);
    }
}
