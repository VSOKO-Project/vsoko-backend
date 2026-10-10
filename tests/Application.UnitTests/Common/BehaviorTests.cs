using Application.Common.Behaviors;
using Application.Common.CQRS;
using Application.Interfaces.DataManager;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AppValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.UnitTests.Common;

public class BehaviorTests
{
    public record TestCommand(string? Value) : IRequest<string>, ICommand;

    public record TestQuery : IRequest<string>, IQuery;

    private class ValueValidator : AbstractValidator<TestCommand>
    {
        public ValueValidator() => RuleFor(x => x.Value).NotEmpty();
    }

    private static RequestHandlerDelegate<string> Returns(string value) => _ => Task.FromResult(value);

    private static RequestHandlerDelegate<string> Throws() => _ => throw new InvalidOperationException("boom");

    [Fact]
    public async Task Transaction_Query_SkipsTransaction()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestQuery, string>(uow);

        var result = await behavior.Handle(new TestQuery(), Returns("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Empty(uow.ReceivedCalls());
    }

    [Fact]
    public async Task Transaction_Command_Commits()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, string>(uow);

        var result = await behavior.Handle(new TestCommand("x"), Returns("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Received.InOrder(() =>
        {
            uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            uow.CommitTransactionAsync(Arg.Any<CancellationToken>());
        });
        await uow.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transaction_CommandFails_RollsBackAndRethrows()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestCommand, string>(uow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestCommand("x"), Throws(), CancellationToken.None));

        await uow.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validation_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, string>([]);

        Assert.Equal("ok", await behavior.Handle(new TestCommand(null), Returns("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Validation_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, string>([new ValueValidator()]);

        Assert.Equal("ok", await behavior.Handle(new TestCommand("x"), Returns("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Validation_InvalidRequest_ThrowsWithFailures()
    {
        var behavior = new ValidationBehavior<TestCommand, string>([new ValueValidator()]);
        var nextCalled = false;

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(new TestCommand(""), _ => { nextCalled = true; return Task.FromResult("ok"); }, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Equal(nameof(TestCommand.Value), Assert.Single(ex.ValidationErrors!).PropertyName);
    }

    [Fact]
    public async Task Logging_PassesResultThrough()
    {
        var behavior = new LoggingBehavior<TestQuery, string>(NullLogger<LoggingBehavior<TestQuery, string>>.Instance);

        Assert.Equal("ok", await behavior.Handle(new TestQuery(), Returns("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Logging_Rethrows()
    {
        var behavior = new LoggingBehavior<TestQuery, string>(NullLogger<LoggingBehavior<TestQuery, string>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestQuery(), Throws(), CancellationToken.None));
    }
}
