using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.TeachersFeatures.Query;

public class GetAllTeachersRequest : IRequest<PagedResultDto<TeacherDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetAllTeachersRequestValidator : AbstractValidator<GetAllTeachersRequest>
{
    public GetAllTeachersRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(w => w.PageSize).NotNull();
    }
}

public class GetAllTeachersRequestHandler
    : IRequestHandler<GetAllTeachersRequest, PagedResultDto<TeacherDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICacheService _cacheService;

    public GetAllTeachersRequestHandler(ITeacherRepository teacherRepository, ICacheService cacheService)
    {
        _teacherRepository = teacherRepository;
        _cacheService = cacheService;
    }

    public async Task<PagedResultDto<TeacherDto>> Handle(
        GetAllTeachersRequest request,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Teacher.GetPaged(request.Page, request.PageSize, request.Query ?? "");
        var tag = CacheKeys.Teacher.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _teacherRepository.GetAllAsync(request.Page, request.Query ?? "", request.PageSize, cancellationToken), [tag], cancellationToken))!;
    }
}
