using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.WorkloadFeatures.Query;

public record GetWorkloadByIdQuery(string Id) : IRequest<WorkloadDto>;

public class GetWorkloadByIdQueryHandler : IRequestHandler<GetWorkloadByIdQuery, WorkloadDto>
{
    private readonly IWorkloadRepository _workloadRepository;

    public GetWorkloadByIdQueryHandler(IWorkloadRepository workloadRepository)
    {
        _workloadRepository = workloadRepository;
    }

    public async Task<WorkloadDto> Handle(GetWorkloadByIdQuery request, CancellationToken cancellationToken)
    {
        return await _workloadRepository.GetWorkloadById(request.Id, cancellationToken);
    }
}
