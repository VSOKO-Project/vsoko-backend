using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.Features.ImportFeatures;

/// <summary>Преподаватели с нагрузкой в семестре: для выбора в выгрузке. Если семестра нет — пустой список.</summary>
public record GetPeriodTeachersQuery(int StartYear, Term Term) : IRequest<List<TeacherDto>>, IQuery;

public class GetPeriodTeachersQueryHandler : IRequestHandler<GetPeriodTeachersQuery, List<TeacherDto>>
{
    private readonly IImportRepository _importRepository;

    public GetPeriodTeachersQueryHandler(IImportRepository importRepository)
    {
        _importRepository = importRepository;
    }

    public async Task<List<TeacherDto>> Handle(GetPeriodTeachersQuery request, CancellationToken cancellationToken)
    {
        return await _importRepository.GetPeriodTeachersAsync(new PeriodKey(request.StartYear, request.Term), cancellationToken);
    }
}
