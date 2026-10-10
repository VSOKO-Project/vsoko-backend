using Application.Features.CriteriaFeatures.Command;
using Application.Features.DisciplineFeatures.Query;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.ImportFeatures;
using Application.Features.PeriodFeatures.Command;
using Application.Features.SecurityFeatures.Command;
using Application.Features.TeachersFeatures.Query;
using Application.Features.WorkloadFeatures;
using Domain.Enums;
using FluentValidation;

namespace Application.UnitTests.Features;

public class ValidatorTests
{
    private static string[] Invalid<T>(IValidator<T> validator, T request) =>
        validator.Validate(request).Errors.Select(e => e.PropertyName).Distinct().ToArray();

    [Fact]
    public void Criteria()
    {
        Assert.Equal(["Id"], Invalid(new DeleteCriteriaCommandValidator(), new DeleteCriteriaCommandRequest()));
        Assert.Empty(Invalid(new DeleteCriteriaCommandValidator(), new DeleteCriteriaCommandRequest { Id = "c1" }));

        Assert.Equal(["Name"], Invalid(new PostCriteriaCommandValidator(), new PostCriteriaCommandRequest()));
        Assert.Empty(Invalid(new PostCriteriaCommandValidator(), new PostCriteriaCommandRequest { Name = "n" }));

        Assert.Equal(["Id", "Name"], Invalid(new PutCriteriaCommandValidator(), new PutCriteriaCommandRequest()));
        Assert.Empty(Invalid(new PutCriteriaCommandValidator(), new PutCriteriaCommandRequest { Id = "c1", Name = "n" }));
    }

    [Fact]
    public void Feedback()
    {
        Assert.Equal(["Feedback", "workloadId"], Invalid(new PostFeedbackRequestValidator(), new PostFeedbackRequest { Feedback = [] }));
        Assert.Empty(Invalid(new PostFeedbackRequestValidator(),
            new PostFeedbackRequest { Feedback = [new Grades { CriteriaId = "c1", Grade = 5 }], workloadId = "w1" }));

        Assert.Equal(["Id"], Invalid(new PutFeedbackRequestValidator(), new PutFeedbackRequest()));
        Assert.Equal(["Id"], Invalid(new DeleteFeedbackRequestValidator(), new DeleteFeedbackRequest { Id = "" }));
        Assert.Empty(Invalid(new DeleteFeedbackRequestValidator(), new DeleteFeedbackRequest { Id = "f1" }));
    }

    [Fact]
    public void Security()
    {
        Assert.Equal(["Login", "Password"], Invalid(new LoginQueryValidator(), new LoginQuery()));
        Assert.Empty(Invalid(new LoginQueryValidator(), new LoginQuery { Login = "u", Password = "p" }));

        Assert.Equal(["Refresh"], Invalid(new RefreshQueryValidator(), new RefreshQuery()));
        Assert.Equal(["Refresh"], Invalid(new LogOutQueryValidator(), new LogOutQuery()));
        Assert.Empty(Invalid(new LogOutQueryValidator(), new LogOutQuery { Refresh = "r" }));

        Assert.Equal(["CurrentPassword", "NewPassword"],
            Invalid(new ChangePasswordCommandValidator(), new ChangePasswordCommand { NewPassword = "12345" }));
        Assert.Empty(Invalid(new ChangePasswordCommandValidator(),
            new ChangePasswordCommand { CurrentPassword = "old", NewPassword = "123456" }));
    }

    [Fact]
    public void Paging()
    {
        Assert.Empty(Invalid(new GetAllTeachersRequestValidator(), new GetAllTeachersRequest()));
        Assert.Empty(Invalid(new GetTeachersRatingRequestValidator(), new GetTeachersRatingRequest()));
        Assert.Empty(Invalid(new GetAllDisciplinesRequestValidator(), new GetAllDisciplinesRequest()));
        Assert.Empty(Invalid(new GetDisciplineRatingRequestValidator(), new GetDisciplineRatingRequest()));
        Assert.Empty(Invalid(new GetAllWorkloadRequestValidator(), new GetAllWorkloadRequest()));
    }

    [Fact]
    public void Period()
    {
        Assert.Equal(["Id"], Invalid(new SetPeriodFeedbackRequestValidator(), new SetPeriodFeedbackRequest()));
        Assert.Empty(Invalid(new SetPeriodFeedbackRequestValidator(), new SetPeriodFeedbackRequest { Id = "p1" }));
    }

    [Fact]
    public void Import_ValidRequest()
    {
        var request = new PreviewImportQuery
        {
            Content = Stream.Null, FileName = "Roster.XLSX", Length = 10, StartYear = 2026, Term = Term.Spring,
        };

        Assert.Empty(Invalid(new PreviewImportQueryValidator(), request));
    }

    [Fact]
    public void Import_InvalidRequest()
    {
        var empty = new ApplyImportCommand { Term = (Term)0 };
        Assert.Equal(["Content", "FileName", "Length", "StartYear", "Term"], Invalid(new ApplyImportCommandValidator(), empty));

        var wrong = new ApplyImportCommand
        {
            Content = Stream.Null, FileName = "roster.csv", Length = ImportFormat.MaxFileSize + 1, StartYear = 1999, Term = Term.Autumn,
        };
        var errors = new ApplyImportCommandValidator().Validate(wrong).Errors;
        Assert.Contains(errors, e => e.ErrorMessage == "Принимается только файл .xlsx");
        Assert.Contains(errors, e => e.ErrorMessage == "Файл больше 5 МБ");
        Assert.Contains(errors, e => e.PropertyName == "StartYear");
    }
}
