using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Interfaces.FileManager;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.FileManager;

public class ReportHistoryService(AppDbContext db, TimeProvider timeProvider) : IReportHistoryService
{
    public async Task SaveAsync(string periodTitle, byte[] content, CancellationToken cancellationToken)
    {
        var created = timeProvider.GetUtcNow().UtcDateTime;
        var entry = new ReportArchive
        {
            PeriodTitle = periodTitle,
            CreatedAtUtc = created,
            SizeBytes = content.LongLength,
            Content = content,
        };
        entry.FileName = $"VSOKO-report-{created:yyyyMMdd-HHmmss}-{entry.Id[..8]}.pdf";
        db.ReportArchives.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ReportHistoryDto>> GetHistoryAsync(int page, CancellationToken cancellationToken)
    {
        const int pageSize = 10;
        page = Math.Clamp(page, 1, 100_000);
        var query = db.ReportArchives.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        return new PagedResultDto<ReportHistoryDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = await query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ReportHistoryDto(r.Id, r.PeriodTitle, r.CreatedAtUtc, r.FileName, r.SizeBytes))
                .ToListAsync(cancellationToken),
        };
    }

    public async Task<ReportFileDto> GetFileAsync(string id, CancellationToken cancellationToken) =>
        await db.ReportArchives.AsNoTracking().Where(r => r.Id == id)
            .Select(r => new ReportFileDto(r.FileName, r.Content)).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Отчёт не найден");
}
