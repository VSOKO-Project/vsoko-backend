using Application.Common.Results;
using Application.Common.ResultsDto;
using Domain.Entities;

namespace Application.UnitTests.Common;

public class EntityTests
{
    [Fact]
    public void BaseEntity_GeneratesIdAndKeepsAudit()
    {
        var now = DateTime.UtcNow;
        var a = new Refresh
        {
            UserId = "u1", Token = "t", SecurityStamp = "s", ExpiresAt = now,
            CreatedAtUtc = now, CreatedById = "c", UpdatedAtUtc = now, UpdatedById = "u", IsDeleted = true,
        };

        Assert.NotEqual(new Refresh().Id, a.Id);
        Assert.True(Guid.TryParse(a.Id, out _));
        Assert.Equal(("u1", "t", "s", now), (a.UserId, a.Token, a.SecurityStamp, a.ExpiresAt));
        Assert.Equal((now, "c", now, "u", true), (a.CreatedAtUtc, a.CreatedById, a.UpdatedAtUtc, a.UpdatedById, a.IsDeleted));
    }

    [Fact]
    public void Navigations_AreSettable()
    {
        var group = new StudentGroup { Name = "ИВТ-21", StudentRefs = [], WorkloadRefs = [] };
        var student = new Student { GroupRef = group, FeedbackRefs = [] };
        var role = new EmployeeRole { Name = "admin", EmployeeRefs = [] };

        Assert.NotNull(group.StudentRefs);
        Assert.NotNull(group.WorkloadRefs);
        Assert.NotNull(student.FeedbackRefs);
        Assert.NotNull(role.EmployeeRefs);
    }

    [Fact]
    public void ResultDtos_KeepValues()
    {
        var expires = DateTime.UtcNow;
        var login = new LoginResultDto { AccessToken = "a", RefreshToken = "r", Expires = expires, UserId = "u", IsAdmin = true, MustChangePassword = true };
        Assert.Equal(("a", "r", expires, "u", true, true),
            (login.AccessToken, login.RefreshToken, login.Expires, login.UserId, login.IsAdmin, login.MustChangePassword));

        var page = new PagedResultDto<int> { Items = [1], TotalPages = 2, PageSize = 1, TotalCount = 2, Page = 1 };
        Assert.Equal((1, 2, 1, 2, 1), (page.Items![0], page.TotalPages, page.PageSize, page.TotalCount, page.Page));
    }
}
