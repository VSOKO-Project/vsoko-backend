using System.ComponentModel;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Interfaces.CachingManager;
using Application.Common.Caching;
using Application.Common.CQRS;

namespace Application.Features.CriteriaFeatures.Command;

public class DeleteCriteriaCommandRequest : IRequest<Unit>, ICommand
{
    public string? Id { get; init; }
}

public class DeleteCriteriaCommandValidator : AbstractValidator<DeleteCriteriaCommandRequest>
{
    public DeleteCriteriaCommandValidator()
    {
        RuleFor(w => w.Id).NotEmpty();
    }
}

public class DeleteCriteriaCommandRequestHandler
    : IRequestHandler<DeleteCriteriaCommandRequest, Unit>
{
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly ICacheService _cacheService;

    public DeleteCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository, ICacheService cacheService)
    {
        _criteriaRepository = criteriaRepository;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(
        DeleteCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var single_key = CacheKeys.Criteria.GetById(request.Id!);
        var tag = CacheKeys.Criteria.ListTag;

        await _criteriaRepository.DeleteCriteria(request.Id!, cancellationToken);

        await _cacheService.RemoveAsync(single_key, cancellationToken);
        await _cacheService.RemoveByTagAsync(tag, cancellationToken);
        
        return Unit.Value;
    }
}
