using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.DisciplineFeatures.Query;

public record GetDisciplineByIdQuery(string Id) : IRequest<DisciplineDto>, IQuery;
public class GetDisciplineByIdQueryHandler(IDisciplineRepository repository) : IRequestHandler<GetDisciplineByIdQuery, DisciplineDto>
{
    public Task<DisciplineDto> Handle(GetDisciplineByIdQuery request, CancellationToken cancellationToken) => repository.GetByIdAsync(request.Id, cancellationToken);
}
