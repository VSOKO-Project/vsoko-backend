using Application.Common.Behaviors;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Specification.FeedbackSpecification;
using Application.Common.Specification.WorkloadSpecification;
using Application.Features.CriteriaFeatures.Command;
using Application.Features.CriteriaFeatures.Query;
using Application.Features.ImportFeatures;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using Application.UnitTests.TestDoubles;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.UnitTests;

/// <summary>Регистрация слоя и полный пайплайн MediatR: логирование → валидация → транзакция → хендлер.</summary>
public class DependencyInjectionTests
{
    private readonly ICriteriaRepository _criteria = Substitute.For<ICriteriaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(_criteria);
        services.AddSingleton(_uow);
        services.AddSingleton<ICacheService, FakeCacheService>();
        services.AddSingleton<IUserContext>(FakeUserContext.Admin());
        services.AddSingleton(Substitute.For<IRosterFileReader>());
        services.AddSingleton(Substitute.For<IImportRepository>());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void RegistersLayerServices()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        Assert.IsType<WorkloadAccessService>(scope.ServiceProvider.GetRequiredService<IWorkloadAccessService>());
        Assert.IsType<FeedbackAccessService>(scope.ServiceProvider.GetRequiredService<IFeedbackAccessService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ImportPlanner>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.NotEmpty(provider.GetServices<FluentValidation.IValidator<PostCriteriaCommandRequest>>());
    }

    [Fact]
    public async Task Command_RunsInTransaction()
    {
        using var provider = Build();
        _criteria.PostCriteria("n", CriteriaObject.Teacher, Arg.Any<CancellationToken>()).Returns(new CriteriaDto { Id = "c1" });

        var result = await provider.GetRequiredService<IMediator>()
            .Send(new PostCriteriaCommandRequest { Name = "n", criteriaObject = CriteriaObject.Teacher });

        Assert.Equal("c1", result.Id);
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidCommand_IsRejectedBeforeTransaction()
    {
        using var provider = Build();

        await Assert.ThrowsAsync<ValidationException>(() =>
            provider.GetRequiredService<IMediator>().Send(new PostCriteriaCommandRequest()));

        Assert.Empty(_uow.ReceivedCalls());
        Assert.Empty(_criteria.ReceivedCalls());
    }

    [Fact]
    public async Task Query_SkipsTransaction()
    {
        using var provider = Build();
        _criteria.GetAllCriteria(Arg.Any<CancellationToken>()).Returns([]);

        await provider.GetRequiredService<IMediator>().Send(new GetAllCriteriaQuery());

        Assert.Empty(_uow.ReceivedCalls());
    }
}
