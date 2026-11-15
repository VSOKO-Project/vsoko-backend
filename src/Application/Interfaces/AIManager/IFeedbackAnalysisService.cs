using Application.Common.DTOs;
using Application.Common.Periods;

namespace Application.Interfaces.AIManager;

public interface IFeedbackAnalysisService
{
    Task<FeedbackReportDto> GetReportAsync(string id, bool teacher, PeriodFilter period, CancellationToken ct);
    Task<List<FeedbackCategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<FeedbackCategoryDto> AddCategoryAsync(AddFeedbackCategoryRequest request, CancellationToken ct);
    Task ReviewSignalAsync(string feedbackId, string signalKey, ReviewFeedbackSignalRequest request, CancellationToken ct);
}
