namespace Application.Interfaces.FileManager;

public interface IReportService
{
   Task<byte[]> GenerateReportAsync(CancellationToken cancellationToken);
}