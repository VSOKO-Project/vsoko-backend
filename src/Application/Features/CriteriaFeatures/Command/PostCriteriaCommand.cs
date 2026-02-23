using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Application.Common.Caching;
using Application.Common.CQRS;

namespace Application.Features.CriteriaFeatures.Command;

public class PostCriteriaCommandRequest : IRequest<CriteriaDto>, ICommand
{
    public string? Name { get; init; }
    public CriteriaObject criteriaObject { get; init; }
}

public class PostCriteriaCommandValidator : AbstractValidator<PostCriteriaCommandRequest>
{
    public PostCriteriaCommandValidator()
    {
        RuleFor(w => w.Name).NotEmpty();
        RuleFor(w => w.criteriaObject).NotNull();
    }
}

public class PostCriteriaCommandRequestHandler : IRequestHandler<PostCriteriaCommandRequest, CriteriaDto>
{
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly ICacheService _cacheService;

    public PostCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository, ICacheService cacheService)
    {
        _criteriaRepository = criteriaRepository;
        _cacheService = cacheService;
    }

    public async Task<CriteriaDto> Handle(
        PostCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Criteria.ListTag;

        var result = await _criteriaRepository.PostCriteria(
            request.Name,
            request.criteriaObject,
            cancellationToken
        );

        await _cacheService.RemoveByTagAsync(key, cancellationToken);

        return result;
    }
}
