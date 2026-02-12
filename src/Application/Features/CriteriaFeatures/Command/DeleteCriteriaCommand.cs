using System.ComponentModel;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.CriteriaFeatures.Command;

public class DeleteCriteriaCommandRequest : IRequest<Unit>
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

    public DeleteCriteriaCommandRequestHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<Unit> Handle(
        DeleteCriteriaCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        await _criteriaRepository.DeleteCriteria(request.Id!, cancellationToken);
        return Unit.Value;
    }
}
