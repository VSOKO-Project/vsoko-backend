using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Features.CriteriaFeatures.Command;
using Application.Features.CriteriaFeatures.Query;
using Application.Interfaces.DataManager.Repositories;
using Application.UnitTests.TestDoubles;
using Domain.Enums;
using MediatR;
using NSubstitute;

namespace Application.UnitTests.Features;

public class CriteriaHandlerTests
{
    private readonly ICriteriaRepository _repo = Substitute.For<ICriteriaRepository>();
    private readonly FakeCacheService _cache = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    [Fact]
    public async Task GetAll_CachesUnderListTag()
    {
        List<CriteriaDto> criteria = [new() { Id = "c1", Name = "n" }];
        _repo.GetAllCriteria(Arg.Any<CancellationToken>()).Returns(criteria);

        var result = await new GetAllCriteriaQueryHandler(_repo, _cache).Handle(new GetAllCriteriaQuery(), _ct);

        Assert.Same(criteria, result);
        Assert.Equal((CacheKeys.Criteria.All, CacheKeys.Criteria.ListTag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task GetById_CachesById()
    {
        var dto = new CriteriaDto { Id = "c1", Name = "n" };
        _repo.GetCriteriaById("c1", Arg.Any<CancellationToken>()).Returns(dto);

        var result = await new GetCriteriaByIdQueryHandler(_repo, _cache).Handle(new GetCriteriaByIdQuery("c1"), _ct);

        Assert.Same(dto, result);
        Assert.Equal(CacheKeys.Criteria.GetById("c1"), Assert.Single(_cache.Requests).Key);
    }

    [Fact]
    public async Task Post_CreatesAndInvalidatesList()
    {
        var dto = new CriteriaDto { Id = "c1", Name = "n" };
        _repo.PostCriteria("n", CriteriaObject.Discipline, _ct).Returns(dto);

        var result = await new PostCriteriaCommandRequestHandler(_repo, _cache)
            .Handle(new PostCriteriaCommandRequest { Name = "n", criteriaObject = CriteriaObject.Discipline }, _ct);

        Assert.Same(dto, result);
        Assert.Equal([CacheKeys.Criteria.ListTag], _cache.RemovedTags);
    }

    [Fact]
    public async Task Put_UpdatesAndInvalidatesListAndItem()
    {
        var dto = new CriteriaDto { Id = "c1", Name = "new" };
        _repo.PutCriteria("c1", "new", CriteriaObject.Teacher, _ct).Returns(dto);

        var result = await new PutCriteriaCommandRequestHandler(_repo, _cache)
            .Handle(new PutCriteriaCommandRequest { Id = "c1", Name = "new", criteriaObject = CriteriaObject.Teacher }, _ct);

        Assert.Same(dto, result);
        Assert.Equal([CacheKeys.Criteria.ListTag], _cache.RemovedTags);
        Assert.Equal([CacheKeys.Criteria.GetById("c1")], _cache.RemovedKeys);
    }

    [Fact]
    public async Task Delete_DeletesAndInvalidatesListAndItem()
    {
        var result = await new DeleteCriteriaCommandRequestHandler(_repo, _cache)
            .Handle(new DeleteCriteriaCommandRequest { Id = "c1" }, _ct);

        Assert.Equal(Unit.Value, result);
        await _repo.Received(1).DeleteCriteria("c1", _ct);
        Assert.Equal([CacheKeys.Criteria.ListTag], _cache.RemovedTags);
        Assert.Equal([CacheKeys.Criteria.GetById("c1")], _cache.RemovedKeys);
    }
}
