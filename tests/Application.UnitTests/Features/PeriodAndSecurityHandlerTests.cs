using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.ResultsDto;
using Application.Features.PeriodFeatures.Command;
using Application.Features.PeriodFeatures.Query;
using Application.Features.SecurityFeatures.Command;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.SecurityManager;
using Application.UnitTests.TestDoubles;
using Domain.Enums;
using MediatR;
using NSubstitute;

namespace Application.UnitTests.Features;

public class PeriodAndSecurityHandlerTests
{
    private readonly IPeriodRepository _periods = Substitute.For<IPeriodRepository>();
    private readonly ISecurityService _security = Substitute.For<ISecurityService>();
    private readonly CancellationToken _ct = CancellationToken.None;

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public async Task SetPeriodFeedback_UpdatesAndInvalidatesWorkloads()
    {
        var cache = new FakeCacheService();
        var dto = new PeriodDto { Id = "p1", IsFeedbackOpen = true };
        _periods.SetFeedbackOpenAsync("p1", true, _ct).Returns(dto);

        var result = await new SetPeriodFeedbackRequestHandler(_periods, cache)
            .Handle(new SetPeriodFeedbackRequest { Id = "p1", IsOpen = true }, _ct);

        Assert.Same(dto, result);
        Assert.Equal([CacheKeys.Workload.ListTag], cache.RemovedTags);
    }

    [Fact]
    public async Task GetAllPeriods_ReturnsRepositoryList()
    {
        List<PeriodListItemDto> list = [new() { Id = "p1" }];
        _periods.GetAllAsync(_ct).Returns(list);

        Assert.Same(list, await new GetAllPeriodsQueryHandler(_periods).Handle(new GetAllPeriodsQuery(), _ct));
    }

    [Fact]
    public async Task SuggestedPeriods_PreviousCurrentNext_MarksExisting()
    {
        _periods.GetAllAsync(_ct).Returns([
            new PeriodListItemDto { Id = "cur", StartYear = 2026, Term = Term.Autumn },
            new PeriodListItemDto { Id = "old", StartYear = 2020, Term = Term.Autumn },
        ]);
        var time = new FixedTime(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

        var result = await new GetSuggestedPeriodsQueryHandler(_periods, time).Handle(new GetSuggestedPeriodsQuery(), _ct);

        Assert.Equal(
            [(2025, Term.Spring, false, false, null), (2026, Term.Autumn, true, true, "cur"), (2026, Term.Spring, false, false, null)],
            result.Select(p => (p.StartYear, p.Term, p.Exists, p.IsCurrent, p.Id)));
    }

    [Fact]
    public async Task Login_PassesCredentials()
    {
        var dto = new LoginResultDto { AccessToken = "a" };
        _security.LoginAsync("u", "p", _ct).Returns(dto);

        Assert.Same(dto, await new LoginQueryHandler(_security).Handle(new LoginQuery { Login = "u", Password = "p" }, _ct));
    }

    [Fact]
    public async Task Login_NullCredentials_BecomeEmpty()
    {
        await new LoginQueryHandler(_security).Handle(new LoginQuery(), _ct);

        await _security.Received(1).LoginAsync("", "", _ct);
    }

    [Fact]
    public async Task Refresh_PassesToken()
    {
        var dto = new LoginResultDto { RefreshToken = "r2" };
        _security.RefreshToken("r1", _ct).Returns(dto);

        Assert.Same(dto, await new RefreshQueryHandler(_security).Handle(new RefreshQuery { Refresh = "r1" }, _ct));
    }

    [Fact]
    public async Task LogOut_RevokesToken()
    {
        Assert.Equal(Unit.Value, await new LogOutQueryHandler(_security).Handle(new LogOutQuery { Refresh = "r1" }, _ct));

        await _security.Received(1).LogOut("r1", _ct);
    }

    [Fact]
    public async Task ChangePassword_UsesCurrentUser()
    {
        var dto = new LoginResultDto { MustChangePassword = false };
        _security.ChangePasswordAsync("u1", "old", "newpass", _ct).Returns(dto);

        var result = await new ChangePasswordCommandHandler(_security, FakeUserContext.Student("u1"))
            .Handle(new ChangePasswordCommand { CurrentPassword = "old", NewPassword = "newpass" }, _ct);

        Assert.Same(dto, result);
    }

    [Fact]
    public async Task ChangePassword_NullPasswords_BecomeEmpty()
    {
        await new ChangePasswordCommandHandler(_security, FakeUserContext.Student("u1")).Handle(new ChangePasswordCommand(), _ct);

        await _security.Received(1).ChangePasswordAsync("u1", "", "", _ct);
    }
}
