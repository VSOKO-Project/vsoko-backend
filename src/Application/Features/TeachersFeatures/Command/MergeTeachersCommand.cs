using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.TeachersFeatures.Command;

/// <summary>
/// Объединение дублей: нагрузка и отзывы источника переходят к целевому преподавателю, источник удаляется.
/// </summary>
public record MergeTeachersCommand(string SourceId, string TargetId) : IRequest<TeacherDto>, ICommand;

public class MergeTeachersCommandValidator : AbstractValidator<MergeTeachersCommand>
{
    public MergeTeachersCommandValidator()
    {
        RuleFor(c => c.SourceId).NotEmpty();
        RuleFor(c => c.TargetId).NotEmpty()
            .NotEqual(c => c.SourceId).WithMessage("Нельзя объединить преподавателя с самим собой");
    }
}

public class MergeTeachersCommandHandler : IRequestHandler<MergeTeachersCommand, TeacherDto>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICacheService _cacheService;

    public MergeTeachersCommandHandler(ITeacherRepository teacherRepository, ICacheService cacheService)
    {
        _teacherRepository = teacherRepository;
        _cacheService = cacheService;
    }

    public async Task<TeacherDto> Handle(MergeTeachersCommand request, CancellationToken cancellationToken)
    {
        var result = await _teacherRepository.MergeAsync(request.SourceId, request.TargetId, cancellationToken);

        await CatalogCache.InvalidateAsync(_cacheService, cancellationToken);
        return result;
    }
}
