using Application.Common.DTOs;
using Application.Common.Results;

namespace Application.Interfaces.FileManager;

public interface IReportHistoryService
{
    Task SaveAsync(string periodTitle, byte[] content, CancellationToken cancellationToken);
    Task<PagedResultDto<ReportHistoryDto>> GetHistoryAsync(int page, CancellationToken cancellationToken);
    Task<ReportFileDto> GetFileAsync(string id, CancellationToken cancellationToken);
}
