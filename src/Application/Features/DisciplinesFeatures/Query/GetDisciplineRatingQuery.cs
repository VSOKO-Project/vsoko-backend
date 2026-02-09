using System.Security.Cryptography.X509Certificates;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Application.Common.Results;
using FluentValidation;
using MediatR;

namespace Application.Features.DisciplineFeatures.Query;

public class GetDisciplineRatingRequest : IRequest<PagedResultDto<RatingDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetDisciplineRatingRequestValidator : AbstractValidator<GetDisciplineRatingRequest>
{
    public GetDisciplineRatingRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(w => w.PageSize).NotNull();
    }
}

public class GetDisciplineRatingRequestHandler : IRequestHandler<GetDisciplineRatingRequest, PagedResultDto<RatingDto>>
{
    private readonly IDisciplineRepository _disciplineRepository;

    public GetDisciplineRatingRequestHandler(IDisciplineRepository disciplineRepository)
    {
        _disciplineRepository = disciplineRepository;
    }

    public async Task<PagedResultDto<RatingDto>> Handle(GetDisciplineRatingRequest request, CancellationToken cancellationToken)
    {
        return await _disciplineRepository.GetRating(request.Page, request.Query ?? "", request.PageSize, cancellationToken);
    }
}