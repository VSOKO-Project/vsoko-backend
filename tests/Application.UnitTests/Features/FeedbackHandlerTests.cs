using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.FeedbackFeatures.Query;
using Application.Interfaces.DataManager.Repositories;
using Application.UnitTests.TestDoubles;
using MediatR;
using NSubstitute;
using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.UnitTests.Features;

public class FeedbackHandlerTests
{
    private static readonly string[] RatingTags =
        [CacheKeys.Teacher.ListTag, CacheKeys.Discipline.ListTag, CacheKeys.Workload.ListTag];

    private readonly IFeedbackRepository _feedback = Substitute.For<IFeedbackRepository>();
    private readonly IWorkloadRepository _workload = Substitute.For<IWorkloadRepository>();
    private readonly FakeCacheService _cache = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private readonly List<Grades> _grades = [new() { CriteriaId = "c1", Grade = 5 }];

    private PostFeedbackRequestHandler PostHandler(FakeUserContext user) => new(_feedback, _workload, user, _cache);

    private PostFeedbackRequest PostRequest() => new() { Feedback = _grades, Comment = "good", workloadId = "w1" };

    [Fact]
    public async Task Post_Saves_AndInvalidatesStudentListAndRatings()
    {
        var dto = new FeedbackDto { Id = "f1" };
        _workload.IsFeedbackPeriodOpenAsync("w1", _ct).Returns(true);
        _feedback.PostFeedback(_grades, "good", "w1", "s1", _ct).Returns(dto);

        var result = await PostHandler(FakeUserContext.Student("s1")).Handle(PostRequest(), _ct);

        Assert.Same(dto, result);
        Assert.Equal([CacheKeys.Feedback.Tag, .. RatingTags], _cache.RemovedTags);
    }

    [Fact]
    public async Task Post_AlreadyLeft_IsRejected()
    {
        _feedback.HasFeedbackAsync("w1", _ct).Returns(true);

        await Assert.ThrowsAsync<ValidationException>(() => PostHandler(FakeUserContext.Student()).Handle(PostRequest(), _ct));

        await _feedback.DidNotReceiveWithAnyArgs().PostFeedback(default!, default, default!, default, default);
    }

    [Fact]
    public async Task Post_PeriodClosed_IsRejected()
    {
        _workload.IsFeedbackPeriodOpenAsync("w1", _ct).Returns(false);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => PostHandler(FakeUserContext.Student()).Handle(PostRequest(), _ct));

        Assert.Equal(FeedbackMessages.PeriodClosed, ex.Message);
        await _feedback.DidNotReceiveWithAnyArgs().PostFeedback(default!, default, default!, default, default);
    }

    [Fact]
    public async Task Post_WithoutUserId_IsUnauthorized()
    {
        _workload.IsFeedbackPeriodOpenAsync("w1", _ct).Returns(true);

        await Assert.ThrowsAsync<UnauthorizationException>(() =>
            PostHandler(new FakeUserContext { Role = "student" }).Handle(PostRequest(), _ct));
    }

    [Fact]
    public async Task Put_Updates_AndInvalidates()
    {
        var dto = new FeedbackDto { Id = "f1" };
        _feedback.IsFeedbackPeriodOpenAsync("f1", _ct).Returns(true);
        _feedback.PutFeedback("f1", "upd", _grades, _ct).Returns(dto);

        var result = await new PutFeedbackRequestHandler(_feedback, FakeUserContext.Student("s1"), _cache)
            .Handle(new PutFeedbackRequest { Id = "f1", Comment = "upd", Feedback = _grades }, _ct);

        Assert.Same(dto, result);
        Assert.Equal([CacheKeys.Feedback.Tag, .. RatingTags], _cache.RemovedTags);
        Assert.Empty(_cache.RemovedKeys);
    }

    [Fact]
    public async Task Put_WithoutGrades_PassesEmptyList()
    {
        _feedback.IsFeedbackPeriodOpenAsync("f1", _ct).Returns(true);

        await new PutFeedbackRequestHandler(_feedback, FakeUserContext.Student(), _cache)
            .Handle(new PutFeedbackRequest { Id = "f1" }, _ct);

        await _feedback.Received(1).PutFeedback("f1", null, Arg.Is<List<Grades>>(g => g.Count == 0), _ct);
    }

    [Fact]
    public async Task Put_PeriodClosed_IsRejected()
    {
        var handler = new PutFeedbackRequestHandler(_feedback, FakeUserContext.Student(), _cache);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new PutFeedbackRequest { Id = "f1" }, _ct));

        Assert.Equal(FeedbackMessages.PeriodClosed, ex.Message);
        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Put_WithoutUserId_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new PutFeedbackRequestHandler(_feedback, new FakeUserContext(), _cache).Handle(new PutFeedbackRequest { Id = "f1" }, _ct));
    }

    [Fact]
    public async Task Delete_Deletes_AndInvalidates()
    {
        var result = await new DeleteFeedbackRequestHandler(_feedback, FakeUserContext.Student("s1"), _cache)
            .Handle(new DeleteFeedbackRequest { Id = "f1" }, _ct);

        Assert.Equal(Unit.Value, result);
        await _feedback.Received(1).DeleteFeedback("f1", _ct);
        Assert.Equal([CacheKeys.Feedback.Tag, .. RatingTags], _cache.RemovedTags);
        Assert.Empty(_cache.RemovedKeys);
    }

    [Fact]
    public async Task Delete_WithoutUserId_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new DeleteFeedbackRequestHandler(_feedback, new FakeUserContext(), _cache).Handle(new DeleteFeedbackRequest { Id = "f1" }, _ct));
    }

    [Fact]
    public async Task GetAll_Student_UsesPersonalKey()
    {
        var page = new PagedResultDto<FeedbackDto> { Items = [] };
        _feedback.GetPagedFeedbacks(2, 5, "d1", "t1", "w1", Arg.Any<CancellationToken>()).Returns(page);
        var query = new GetAllFeedbackQuery { Page = 2, PageSize = 5, DisciplineId = "d1", TeacherId = "t1", WorkloadId = "w1" };

        var result = await new GetAllFeedbackQueryHandler(_feedback, FakeUserContext.Student("s1"), _cache).Handle(query, _ct);

        Assert.Same(page, result);
        var request = Assert.Single(_cache.Requests);
        Assert.Equal(CacheKeys.Feedback.GetPagedForStudent(2, 5, "s1", "d1", "t1", "w1"), request.Key);
        Assert.Equal(CacheKeys.Feedback.Tag, request.Tags);
    }

    [Fact]
    public async Task GetAll_Admin_UsesSharedKey()
    {
        await new GetAllFeedbackQueryHandler(_feedback, FakeUserContext.Admin(), _cache).Handle(new GetAllFeedbackQuery(), _ct);

        // Тег общий со студенческими выборками, иначе изменения студентов не сбрасывают кэш админа.
        Assert.Equal((CacheKeys.Feedback.GetPaged(1, 10), CacheKeys.Feedback.Tag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task GetAll_WithoutUserId_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new GetAllFeedbackQueryHandler(_feedback, new FakeUserContext(), _cache).Handle(new GetAllFeedbackQuery(), _ct));
    }

    [Fact]
    public async Task GetById_CachesPerStudent()
    {
        var dto = new FeedbackDto { Id = "f1" };
        _feedback.GetFeedbackById("f1", Arg.Any<CancellationToken>()).Returns(dto);

        var result = await new GetFeedbackByIdQueryHandler(_feedback, FakeUserContext.Student("s1"), _cache)
            .Handle(new GetFeedbackByIdQuery("f1"), _ct);

        Assert.Same(dto, result);
        Assert.Equal((CacheKeys.Feedback.GetById("f1", "s1"), CacheKeys.Feedback.Tag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task GetById_WithoutUserId_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new GetFeedbackByIdQueryHandler(_feedback, new FakeUserContext(), _cache).Handle(new GetFeedbackByIdQuery("f1"), _ct));
    }
}
