using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.DisciplineFeatures.Command;

/// <summary>
/// Объединение дублей: нагрузка и отзывы источника переходят к целевой дисциплине, источник удаляется.
/// </summary>
public record MergeDisciplinesCommand(string SourceId, string TargetId) : IRequest<DisciplineDto>, ICommand;

public class MergeDisciplinesCommandValidator : AbstractValidator<MergeDisciplinesCommand>
{
    public MergeDisciplinesCommandValidator()
    {
        RuleFor(c => c.SourceId).NotEmpty();
        RuleFor(c => c.TargetId).NotEmpty()
            .NotEqual(c => c.SourceId).WithMessage("Нельзя объединить дисциплину с самой собой");
    }
}

public class MergeDisciplinesCommandHandler : IRequestHandler<MergeDisciplinesCommand, DisciplineDto>
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ICacheService _cacheService;

    public MergeDisciplinesCommandHandler(IDisciplineRepository disciplineRepository, ICacheService cacheService)
    {
        _disciplineRepository = disciplineRepository;
        _cacheService = cacheService;
    }

    public async Task<DisciplineDto> Handle(MergeDisciplinesCommand request, CancellationToken cancellationToken)
    {
        var result = await _disciplineRepository.MergeAsync(request.SourceId, request.TargetId, cancellationToken);

        await CatalogCache.InvalidateAsync(_cacheService, cancellationToken);
        return result;
    }
}
