using System.ComponentModel;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CriteriaFeatures.Command;

public class PutCriteriaCommandRequest : IRequest<CriteriaDto>
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

    public PutCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<CriteriaDto> Handle(
        PutCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        return await _criteriaRepository.PutCriteria(
            request.Id!,
            request.Name,
            request.criteriaObject,
            cancellationToken
        );
    }
}
