using coo.Application.Common.DTOs;
using coo.Application.Common.Interfaces.DataManager;
using coo.Application.Common.Results;
using coo.Domain.Entities;
using MediatR;
using coo.Application.Common.Extension;
using coo.Application.Common.CQRS;
using FluentValidation;
using System.Data;

namespace coo.Application.Features.WorkloadFeatures;

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

public class GetAllWorkloadRequestHandler : IRequestHandler<GetAllWorkloadRequest, PagedResultDto<WorkloadDto>>
{
    private readonly IWorkloadRepository _workloadRepository;
    public GetAllWorkloadRequestHandler(IWorkloadRepository workloadRepository)
    {
        _workloadRepository = workloadRepository;
    }

    public async Task<PagedResultDto<WorkloadDto>> Handle(GetAllWorkloadRequest request, CancellationToken cancellationToken)
    {
        return await _workloadRepository.GetPagedWorkload(request.Page, request.Query ?? "", request.PageSize, cancellationToken);
    }
}