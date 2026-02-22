using System.Data;
using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Extension;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.WorkloadFeatures;

public class GetAllWorkloadRequest : IRequest<PagedResultDto<WorkloadDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetAllWorkloadRequestValidator : AbstractValidator<GetAllWorkloadRequest>
{
    public GetAllWorkloadRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(w => w.PageSize).NotNull();
    }
}

public class GetAllWorkloadRequestHandler
    : IRequestHandler<GetAllWorkloadRequest, PagedResultDto<WorkloadDto>>
{
    private readonly IWorkloadRepository _workloadRepository;
    private readonly ICacheService _cacheService;
    private readonly IUserContext _userContext;

    public GetAllWorkloadRequestHandler(IWorkloadRepository workloadRepository, ICacheService cacheService, IUserContext userContext)
    {
        _workloadRepository = workloadRepository;
        _cacheService = cacheService;
        _userContext = userContext;
    }

    public async Task<PagedResultDto<WorkloadDto>> Handle(
        GetAllWorkloadRequest request,
        CancellationToken cancellationToken
    )
    {
        var role = _userContext.Role;
        var groupId = _userContext.StudentGroup;

        var key = role == "student" 
            ? CacheKeys.Workload.GetPagedForStudent(request.Page, request.PageSize, groupId)
            : CacheKeys.Workload.GetPaged(request.Page, request.PageSize);
            
        var tag = CacheKeys.Workload.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _workloadRepository.GetPagedWorkload(request.Page, request.Query ?? "", request.PageSize, cancellationToken), [tag], cancellationToken))!;
    }
}
