using Application.Common.DTOs;

namespace Application.Common.Exceptions;

/// <summary>В файле импорта есть ошибки, ничего не записано. Отчёт уходит клиенту вместе с 400.</summary>
public class ImportValidationException : ValidationException
{
    public ImportReportDto Report { get; }

    public ImportValidationException(ImportReportDto report)
        : base("В файле есть ошибки, импорт не выполнен")
    {
        Report = report;
    }
}
