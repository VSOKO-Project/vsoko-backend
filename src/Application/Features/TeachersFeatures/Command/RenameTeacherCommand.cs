using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Features.ImportFeatures;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.TeachersFeatures.Command;

/// <summary>Исправление ФИО. Если такое ФИО уже есть у другого преподавателя, записи нужно объединить.</summary>
public record RenameTeacherCommand(string Id, string Surname, string Name, string Patronymic) : IRequest<TeacherDto>, ICommand;

public class RenameTeacherCommandValidator : AbstractValidator<RenameTeacherCommand>
{
    public RenameTeacherCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Surname).NotEmpty().MaximumLength(255).WithName("Фамилия");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255).WithName("Имя");
        RuleFor(c => c.Patronymic).NotEmpty().MaximumLength(255).WithName("Отчество");
    }
}

public class RenameTeacherCommandHandler : IRequestHandler<RenameTeacherCommand, TeacherDto>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICacheService _cacheService;

    public RenameTeacherCommandHandler(ITeacherRepository teacherRepository, ICacheService cacheService)
    {
        _teacherRepository = teacherRepository;
        _cacheService = cacheService;
    }

    public async Task<TeacherDto> Handle(RenameTeacherCommand request, CancellationToken cancellationToken)
    {
        var result = await _teacherRepository.RenameAsync(
            request.Id,
            ImportPlanBuilder.Normalize(request.Surname),
            ImportPlanBuilder.Normalize(request.Name),
            ImportPlanBuilder.Normalize(request.Patronymic),
            cancellationToken
        );

        await CatalogCache.InvalidateAsync(_cacheService, cancellationToken);
        return result;
    }
}
