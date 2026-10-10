using Application.Common.DTOs;
using Application.Common.Exceptions;
using Domain.Common.Exceptions;
using FluentValidation.Results;
using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.UnitTests.Common;

public class ExceptionTests
{
    [Fact]
    public void NotFound_Constructors()
    {
        Assert.Equal("Teacher42", new NotFoundException("Teacher", "42").Message);
        Assert.Equal("Teacher", new NotFoundException("Teacher").Message);
        Assert.Equal("The object was not found.", new NotFoundException().Message);
    }

    [Fact]
    public void NotFound_CreateMessage_AddsTypeAndId()
    {
        var withType = NotFoundException.CreateMessage("Teacher");
        Assert.Contains("type: Teacher", withType);
        Assert.DoesNotContain("id:", withType);

        Assert.Contains("id: 7", NotFoundException.CreateMessage("Teacher", 7));
    }

    [Fact]
    public void Unauthorization_Constructors()
    {
        Assert.Equal("nope", new UnauthorizationException("nope").Message);
        Assert.NotNull(new UnauthorizationException().Message);
    }

    [Fact]
    public void Validation_WithoutFailures_HasGenericMessage()
    {
        Assert.Equal("Validation error.", new ValidationException().Message);
        Assert.Equal("Validation error.", new ValidationException(Array.Empty<ValidationFailure>()).Message);
        Assert.Equal("custom", new ValidationException("custom").Message);
    }

    [Fact]
    public void Validation_WithFailures_ListsThem()
    {
        var failures = new[] { new ValidationFailure("Name", "empty"), new ValidationFailure("Id", "bad") };

        var ex = new ValidationException(failures);

        Assert.Same(failures, ex.ValidationErrors);
        Assert.Contains("- Name: empty", ex.Message);
        Assert.Contains("- Id: bad", ex.Message);
    }

    [Fact]
    public void ImportValidation_CarriesReport()
    {
        var report = new ImportReportDto();

        var ex = new ImportValidationException(report);

        Assert.Same(report, ex.Report);
        Assert.IsAssignableFrom<ValidationException>(ex);
    }

    [Fact]
    public void Domain_Constructors()
    {
        var empty = new DomainException();
        Assert.Null(empty.ValidationErrors);
        Assert.Equal("Validation error at the Domain level.", empty.Message);

        var single = new DomainException("Name", ["too long", "empty"]);
        Assert.Equal(["too long", "empty"], single.ValidationErrors!["Name"]);
        Assert.Contains("- Name: too long, empty", single.Message);

        var dict = new Dictionary<string, IEnumerable<string>> { ["Id"] = ["bad"] };
        var many = new DomainException(dict);
        Assert.Same(dict, many.ValidationErrors);
        Assert.Contains("- Id: bad", many.Message);

        Assert.Equal("Validation error at the Domain level.",
            DomainException.CreateMessage(new Dictionary<string, IEnumerable<string>>()));
    }
}
