using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.ImportFeatures;

/// <summary>Группы для выбора в выгрузке логинов и паролей.</summary>
public record GetImportGroupsQuery : IRequest<List<StudentGroupDto>>, IQuery;

public class GetImportGroupsQueryHandler : IRequestHandler<GetImportGroupsQuery, List<StudentGroupDto>>
{
    private readonly IImportRepository _importRepository;

    public GetImportGroupsQueryHandler(IImportRepository importRepository)
    {
        _importRepository = importRepository;
    }

    public async Task<List<StudentGroupDto>> Handle(GetImportGroupsQuery request, CancellationToken cancellationToken)
    {
        return await _importRepository.GetGroupsAsync(cancellationToken);
    }
}
