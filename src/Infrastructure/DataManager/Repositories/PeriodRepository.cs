using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Repositories;

public class PeriodRepository : IPeriodRepository
{
    private readonly AppDbContext _dbContext;
    private readonly AcademicPeriodMapper _mapper;

    public PeriodRepository(AppDbContext dbContext, AcademicPeriodMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<PeriodListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = _dbContext.AcademicPeriods
            .OrderByDescending(p => p.StartYear)
            .ThenByDescending(p => p.Term);

        return await _mapper.ProjectToListItem(query).ToListAsync(cancellationToken);
    }

    public async Task<PeriodDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var period = await _mapper.ProjectToDto(_dbContext.AcademicPeriods)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return period ?? throw new NotFoundException(nameof(AcademicPeriod), id);
    }

    public async Task<PeriodDto?> GetOpenAsync(CancellationToken cancellationToken)
    {
        return await _mapper.ProjectToDto(_dbContext.AcademicPeriods.Where(p => p.IsFeedbackOpen))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PeriodDto> SetFeedbackOpenAsync(string id, bool isOpen, CancellationToken cancellationToken)
    {
        var period = await _dbContext.AcademicPeriods.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicPeriod), id);

        if (isOpen)
        {
            var opened = await _dbContext.AcademicPeriods
                .Where(p => p.IsFeedbackOpen && p.Id != id)
                .ToListAsync(cancellationToken);

            foreach (var other in opened)
                other.IsFeedbackOpen = false;

            // Сначала закрываем остальные: уникальный индекс проверяется построчно,
            // а порядок UPDATE внутри одного SaveChanges не гарантирован.
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        period.IsFeedbackOpen = isOpen;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.MapSingle(period);
    }
}
