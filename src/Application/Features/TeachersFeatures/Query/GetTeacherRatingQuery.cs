using System.ComponentModel;
using System.Data;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Application.Common.Results;
using FluentValidation;
using MediatR;

namespace Application.Features.TeachersFeatures.Query;

public class GetTeachersRatingRequest : IRequest<PagedResultDto<RatingDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetTeachersRatingRequestValidator : AbstractValidator<GetTeachersRatingRequest>
{
    public GetTeachersRatingRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(W => W.PageSize).NotNull();
    }
}

public class GetTeachersRatingRequestHandler : IRequestHandler<GetTeachersRatingRequest, PagedResultDto<RatingDto>>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetTeachersRatingRequestHandler(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<PagedResultDto<RatingDto>> Handle(GetTeachersRatingRequest request, CancellationToken cancellationToken)
    {
        return await _teacherRepository.GetRating(request.Page, request.Query ?? "", request.PageSize, cancellationToken);
    }
}