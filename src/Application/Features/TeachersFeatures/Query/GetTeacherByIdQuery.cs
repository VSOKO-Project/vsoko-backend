using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.TeachersFeatures.Query;

public record GetTeacherByIdQuery(string Id) : IRequest<TeacherDto>, IQuery;
public class GetTeacherByIdQueryHandler(ITeacherRepository repository) : IRequestHandler<GetTeacherByIdQuery, TeacherDto>
{
    public Task<TeacherDto> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken) => repository.GetByIdAsync(request.Id, cancellationToken);
}
