using System.Security.Claims;
using Application.Common.Exceptions;
using Infrastructure.FileManager;
using Microsoft.AspNetCore.Http;
using Presentation.Common;
using Presentation.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Api.IntegrationTests;

public class PresentationUnitTests
{
    private static UserContext ContextWith(params Claim[] claims)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new UserContext(new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public void UserContext_ReadsClaims()
    {
        var student = ContextWith(
            new Claim(ClaimTypes.Name, "student1"),
            new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim(ClaimTypes.Role, "student"),
            new Claim("group", "g1"));

        Assert.Equal(("student1", "u1", "student", "g1"), (student.UserName, student.UserId, student.Role, student.StudentGroup));
        Assert.Equal("admin", ContextWith(new Claim(ClaimTypes.Role, "student"), new Claim(ClaimTypes.Role, "Admin")).Role);
    }

    [Fact]
    public void UserContext_WithoutUserOrRoles_ReturnsNulls()
    {
        var none = new UserContext(new HttpContextAccessor());
        Assert.Null(none.UserName);
        Assert.Null(none.UserId);
        Assert.Null(none.Role);
        Assert.Null(none.StudentGroup);

        Assert.Null(ContextWith().Role);
    }

    [Fact]
    public void UserContext_UnknownRole_IsUnauthorized()
    {
        Assert.Throws<UnauthorizationException>(() => ContextWith(new Claim(ClaimTypes.Role, "employee")).Role);
    }

    [Fact]
    public void CorsOptions_SplitsOrigins()
    {
        Assert.Equal(["http://a", "http://b"], new CorsOptions { AllowedOrigins = "http://a,,http://b" }.GetOrigins());
        Assert.Empty(new CorsOptions().GetOrigins());
    }

    [Fact]
    public void PdfExtensions_RenderTableCells()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(doc => doc.Page(page => page.Content().Column(col =>
        {
            col.Item().TableHeader().Text("Заголовок");
            col.Item().TableCell().Text("Ячейка");
        }))).GeneratePdf();

        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }
}
