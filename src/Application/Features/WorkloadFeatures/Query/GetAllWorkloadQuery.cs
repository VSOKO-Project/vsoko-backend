using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Application.Common.Results;
using Domain.Entities;
using MediatR;
using Application.Common.Extension;
using Application.Common.CQRS;
using FluentValidation;
using System.Data;

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