using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Application.Common.Caching;
using Application.Common.CQRS;

namespace Application.Features.CriteriaFeatures.Command;

public class PutCriteriaCommandRequest : IRequest<CriteriaDto>, ICommand
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public CriteriaObject criteriaObject { get; init; }
}

public class PutCriteriaCommandValidator : AbstractValidator<PutCriteriaCommandRequest>
{
    public PutCriteriaCommandValidator()
    {
        RuleFor(w => w.Id).NotEmpty();
        RuleFor(w => w.Name).NotEmpty();
        RuleFor(w => w.criteriaObject).NotNull();
    }
}

public class PutCriteriaCommandRequestHandler : IRequestHandler<PutCriteriaCommandRequest, CriteriaDto>
{
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly ICacheService _cacheService;

    public PutCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository, ICacheService cacheService)
    {
        _criteriaRepository = criteriaRepository;
        _cacheService = cacheService;
    }

    public async Task<CriteriaDto> Handle(
        PutCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Criteria.ListTag;
        var key_single = CacheKeys.Criteria.GetById(request.Id!);

        await _cacheService.RemoveByTagAsync(key, cancellationToken);
        await _cacheService.RemoveAsync(key_single);

        return await _criteriaRepository.PutCriteria(
            request.Id!,
            request.Name,
            request.criteriaObject,
            cancellationToken
        );
    }
}
