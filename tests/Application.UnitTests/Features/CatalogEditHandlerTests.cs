using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Features.DisciplineFeatures.Command;
using Application.Features.TeachersFeatures.Command;
using Application.Interfaces.DataManager.Repositories;
using Application.UnitTests.TestDoubles;
using NSubstitute;

namespace Application.UnitTests.Features;

/// <summary>Переименование и объединение преподавателей и дисциплин.</summary>
public class CatalogEditHandlerTests
{
    private readonly ITeacherRepository _teachers = Substitute.For<ITeacherRepository>();
    private readonly IDisciplineRepository _disciplines = Substitute.For<IDisciplineRepository>();
    private readonly FakeCacheService _cache = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static readonly string[] CatalogTags =
        [CacheKeys.Teacher.ListTag, CacheKeys.Discipline.ListTag, CacheKeys.Workload.ListTag, CacheKeys.Feedback.Tag];

    [Fact]
    public async Task RenameTeacher_NormalizesSpaces_AndResetsCatalogCache()
    {
        var dto = new TeacherDto { Id = "t1" };
        _teachers.RenameAsync("t1", "Петрова", "Анна", "Ивановна", _ct).Returns(dto);

        var result = await new RenameTeacherCommandHandler(_teachers, _cache)
            .Handle(new RenameTeacherCommand("t1", " Петрова ", "Анна", "Ивановна  "), _ct);

        Assert.Same(dto, result);
        Assert.Equal(CatalogTags, _cache.RemovedTags);
    }

    [Fact]
    public async Task MergeTeachers_ResetsCatalogCache()
    {
        var dto = new TeacherDto { Id = "t2" };
        _teachers.MergeAsync("t1", "t2", _ct).Returns(dto);

        var result = await new MergeTeachersCommandHandler(_teachers, _cache).Handle(new MergeTeachersCommand("t1", "t2"), _ct);

        Assert.Same(dto, result);
        Assert.Equal(CatalogTags, _cache.RemovedTags);
    }

    [Fact]
    public async Task RenameDiscipline_NormalizesSpaces_AndResetsCatalogCache()
    {
        var dto = new DisciplineDto { Id = "d1" };
        _disciplines.RenameAsync("d1", "Линейная алгебра", _ct).Returns(dto);

        var result = await new RenameDisciplineCommandHandler(_disciplines, _cache)
            .Handle(new RenameDisciplineCommand("d1", " Линейная   алгебра "), _ct);

        Assert.Same(dto, result);
        Assert.Equal(CatalogTags, _cache.RemovedTags);
    }

    [Fact]
    public async Task MergeDisciplines_ResetsCatalogCache()
    {
        var dto = new DisciplineDto { Id = "d2" };
        _disciplines.MergeAsync("d1", "d2", _ct).Returns(dto);

        var result = await new MergeDisciplinesCommandHandler(_disciplines, _cache).Handle(new MergeDisciplinesCommand("d1", "d2"), _ct);

        Assert.Same(dto, result);
        Assert.Equal(CatalogTags, _cache.RemovedTags);
    }

    [Fact]
    public void Validators_RejectEmptyNames_AndSelfMerge()
    {
        Assert.False(new RenameTeacherCommandValidator().Validate(new RenameTeacherCommand("t1", "Петрова", "", "Ивановна")).IsValid);
        Assert.True(new RenameTeacherCommandValidator().Validate(new RenameTeacherCommand("t1", "Петрова", "Анна", "Ивановна")).IsValid);
        Assert.False(new RenameDisciplineCommandValidator().Validate(new RenameDisciplineCommand("d1", new string('ф', 101))).IsValid);
        Assert.False(new MergeTeachersCommandValidator().Validate(new MergeTeachersCommand("t1", "t1")).IsValid);
        Assert.True(new MergeTeachersCommandValidator().Validate(new MergeTeachersCommand("t1", "t2")).IsValid);
        Assert.False(new MergeDisciplinesCommandValidator().Validate(new MergeDisciplinesCommand("d1", "d1")).IsValid);
    }
}
