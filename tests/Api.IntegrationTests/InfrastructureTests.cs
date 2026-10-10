using Api.IntegrationTests.Support;
using Application.Common.Exceptions;
using Application.Interfaces.DataManager;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using ClosedXML.Excel;
using Infrastructure;
using Infrastructure.AIManager;
using Infrastructure.CachingManager;
using Infrastructure.DataManager;
using Infrastructure.FileManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Api.IntegrationTests;

/// <summary>Компоненты Infrastructure, которые неудобно проверять через HTTP.</summary>
[Collection(ApiCollection.Name)]
public class InfrastructureTests(ApiFactory factory)
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    [Fact]
    public void RosterFile_RoundTrip_SanitizesSheetNames()
    {
        var roster = new RosterFile();
        var long_ = new string('Я', 40);

        var bytes = roster.Write([
            new SpreadsheetSheet("A/B", ["h1", "h2"], [["1", "x"]]),
            new SpreadsheetSheet("a/b", ["h"], []),
            new SpreadsheetSheet(long_, ["h"], [["v"]]),
            new SpreadsheetSheet(long_, ["h"], [["v"]]),
            new SpreadsheetSheet("''", [], []),
        ]);
        var sheets = roster.Read(new MemoryStream(bytes));

        Assert.Equal(["A_B", "a_b (2)", long_[..31], long_[..27] + " (2)", "Лист"], sheets.Select(s => s.Name));
        Assert.Equal(["h1", "h2"], sheets[0].Headers);
        Assert.Equal(["1", "x"], sheets[0].Rows.Single().Values);
        Assert.Empty(sheets[4].Headers);
    }

    [Fact]
    public void RosterFile_EmptyWorkbook_AndNumericCells()
    {
        var roster = new RosterFile();

        var empty = roster.Read(new MemoryStream(roster.Write([])));
        Assert.Equal("Нет данных", Assert.Single(empty).Name);

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Студенты");
        ws.Cell(2, 1).Value = "Номер";
        ws.Cell(3, 1).Value = 210001;
        ws.Cell(4, 1).Value = 1.5;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var sheet = Assert.Single(roster.Read(stream));
        Assert.Equal(["Номер"], sheet.Headers);
        Assert.Equal([["210001"], ["1.5"]], sheet.Rows.Select(r => r.Values));
        Assert.Equal([3, 4], sheet.Rows.Select(r => r.RowNumber));
    }

    [Fact]
    public void RosterFile_Garbage_IsValidationError()
    {
        Assert.Throws<ValidationException>(() => new RosterFile().Read(new MemoryStream([1, 2, 3])));
    }

    [Fact]
    public async Task FeedbackSummarizer_HandlesEmptyAndFailingAi()
    {
        var chat = new FakeChatCompletion();
        var summarizer = new FeedbackSummarizer(chat.CreateKernel());
        var comments = Enumerable.Range(0, 60).Select(i => $"c{i}").ToList();

        Assert.Equal("Сводка от ИИ", await summarizer.SummarizeTeacherAsync("Иванов", comments, default));
        var prompt = chat.Requests.Single().Last().Content!;
        Assert.Contains("c49", prompt);
        Assert.DoesNotContain("c50", prompt);

        chat.Respond = _ => " ";
        Assert.Equal("Empty message from AI.", await summarizer.SummarizeDisciplineAsync("Физика", [], default));

        chat.Respond = _ => throw new HttpOperationException(System.Net.HttpStatusCode.TooManyRequests, "quota", "boom", null);
        Assert.Equal("API Error: TooManyRequests. Details: quota", await summarizer.SummarizeDisciplineAsync("Физика", [], default));

        chat.Respond = _ => throw new InvalidOperationException("offline");
        Assert.Contains("offline", await summarizer.SummarizeTeacherAsync("Иванов", [], default));
    }

    [Fact]
    public void DependencyInjection_RequiresSettings()
    {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().ApplyAiManager(Config()));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddCaching(Config()));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().ApplyDataManager(Config()));

        var services = new ServiceCollection().ApplyAiManager(Config(("AI:ApiKey", "k"), ("AI:ProxyUrl", "http://127.0.0.1:9")));
        Assert.Contains(services, s => s.ServiceType == typeof(Kernel));
    }

    [Fact]
    public void DesignTimeFactory_BuildsNpgsqlContext()
    {
        using var context = new AppDbContextFactory().CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Fact]
    public async Task DatabaseInitializer_LogsAndRethrows()
    {
        var provider = new ServiceCollection().AddLogging().BuildServiceProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.InitializeDatabaseAsync());
    }

    [Fact]
    public async Task DatabaseInitializer_IsIdempotent()
    {
        await factory.Services.InitializeDatabaseAsync();
    }

    [Fact]
    public async Task UnitOfWork_WithoutTransaction_IsNoOp_AndRollbackDiscards()
    {
        await factory.WithScopeAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.CommitTransactionAsync(default);
            await uow.RollbackTransactionAsync(default);

            var db = sp.GetRequiredService<Infrastructure.DataManager.Contexts.AppDbContext>();
            var name = ApiFactory.Unique("Откат ");
            await uow.BeginTransactionAsync(default);
            db.Disciplines.Add(new Domain.Entities.Discipline { Name = name });
            await db.SaveChangesAsync();
            await uow.RollbackTransactionAsync(default);

            db.ChangeTracker.Clear();
            Assert.False(db.Disciplines.Any(d => d.Name == name));
            return true;
        });
    }

    [Fact]
    public async Task PeriodRepository_GetOpenAndById()
    {
        await factory.WithScopeAsync(async sp =>
        {
            var periods = sp.GetRequiredService<IPeriodRepository>();
            var open = await periods.GetOpenAsync(default);

            Assert.NotNull(open);
            Assert.Equal(open.Id, (await periods.GetByIdAsync(open.Id, default)).Id);
            await Assert.ThrowsAsync<NotFoundException>(() => periods.GetByIdAsync("missing", default));
            return true;
        });
    }
}
