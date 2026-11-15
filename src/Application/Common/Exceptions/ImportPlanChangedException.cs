using Application.Common.DTOs;

namespace Application.Common.Exceptions;

public class ImportPlanChangedException(ImportReportDto report) : Exception(
    "План импорта изменился. Проверьте новый отчёт и подтвердите импорт ещё раз.")
{
    public ImportReportDto Report { get; } = report;
}
