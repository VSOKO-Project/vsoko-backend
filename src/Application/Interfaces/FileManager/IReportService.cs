using Application.Common.Periods;

namespace Application.Interfaces.FileManager;

public interface IReportService
{
   Task<byte[]> GenerateReportAsync(PeriodFilter period, CancellationToken cancellationToken);
}
