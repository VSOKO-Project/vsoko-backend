using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Features.ImportFeatures;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.DisciplineFeatures.Command;

/// <summary>Исправление названия. Если такое название уже есть у другой дисциплины, записи нужно объединить.</summary>
public record RenameDisciplineCommand(string Id, string Name) : IRequest<DisciplineDto>, ICommand;

public class RenameDisciplineCommandValidator : AbstractValidator<RenameDisciplineCommand>
{
    public RenameDisciplineCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100).WithName("Название");
    }
}

public class RenameDisciplineCommandHandler : IRequestHandler<RenameDisciplineCommand, DisciplineDto>
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ICacheService _cacheService;

    public RenameDisciplineCommandHandler(IDisciplineRepository disciplineRepository, ICacheService cacheService)
    {
        _disciplineRepository = disciplineRepository;
        _cacheService = cacheService;
    }

    public async Task<DisciplineDto> Handle(RenameDisciplineCommand request, CancellationToken cancellationToken)
    {
        var result = await _disciplineRepository.RenameAsync(request.Id, ImportPlanBuilder.Normalize(request.Name), cancellationToken);

        await CatalogCache.InvalidateAsync(_cacheService, cancellationToken);
        return result;
    }
}
