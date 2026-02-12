using System.ComponentModel;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CriteriaFeatures.Command;

public class PostCriteriaCommandRequest : IRequest<CriteriaDto>
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

    public PostCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<CriteriaDto> Handle(
        PostCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        return await _criteriaRepository.PostCriteria(
            request.Name,
            request.criteriaObject,
            cancellationToken
        );
    }
}
