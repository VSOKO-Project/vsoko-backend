using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.WorkloadFeatures.Query;

public record GetWorkloadByIdQuery(string Id) : IRequest<WorkloadDto>;

public class GetWorkloadByIdQueryHandler : IRequestHandler<GetWorkloadByIdQuery, WorkloadDto>
{
    private readonly IWorkloadRepository _workloadRepository;
    private readonly ICacheService _cacheService;
    private readonly IUserContext _userContext;

    public GetWorkloadByIdQueryHandler(IWorkloadRepository workloadRepository, ICacheService cacheService, IUserContext userContext)
    {
        _workloadRepository = workloadRepository;
        _cacheService = cacheService;
        _userContext = userContext;
    }

    public async Task<WorkloadDto> Handle(GetWorkloadByIdQuery request, CancellationToken cancellationToken)
    {
        var role = _userContext.Role;
        var groupId = _userContext.StudentGroup ?? "";

        var key = role == "student"
            ? CacheKeys.Workload.GetByIdForStudent(request.Id, groupId)
            : CacheKeys.Workload.GetById(request.Id);

        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) => await _workloadRepository.GetWorkloadById(request.Id, ct),
            cancellationToken: cancellationToken))!;
    }
}
