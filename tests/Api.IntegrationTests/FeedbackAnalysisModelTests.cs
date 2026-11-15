using System.Text.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Domain.Entities;
using Infrastructure.AIManager;

namespace Api.IntegrationTests;

// Pure tests: no containers or live model required.
public class FeedbackAnalysisModelTests
{
    private static List<FeedbackCategoryDto> Categories => FeedbackCategoryCatalog.Defaults
        .Select(c => new FeedbackCategoryDto(c.Id, c.Name, c.Description, c.RequiresAttention)).ToList();

    private static ExtractedFeedbackFinding Finding(string quote = "понятно", string category = "explanation", string sentiment = "Positive") =>
        new() { CategoryId = category, Quote = quote, Sentiment = sentiment, Basis = "Personal", RequiresAttention = category == "humiliation" };

    private static FeedbackAnalysis Analysis(string id, string comment, params ExtractedFeedbackFinding[] findings) => new()
    {
        FeedbackId = id, SourceComment = comment, Version = "v1", Status = "Completed",
        FindingsJson = JsonSerializer.Serialize(findings, FeedbackAnalysisModel.JsonOptions)
    };

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(199)]
    public void RareSignalAmong200ReviewsSurvivesAtAnyPosition(int position)
    {
        var sources = Enumerable.Range(0, 200).Select(i => new FeedbackReportSource($"f{i}",
            i == position ? "понятно, но перед группой назвал меня тупым" : "понятно")).ToList();
        var analyses = sources.ToDictionary(s => s.Id, s => Analysis(s.Id, s.Comment, Finding()));
        analyses[$"f{position}"] = Analysis($"f{position}", sources[position].Comment, Finding(),
            Finding("перед группой назвал меня тупым", "humiliation", "Negative"));

        var report = FeedbackReportBuilder.Build("Преподаватель", sources, analyses, [], Categories, "v1");

        Assert.Equal(200, report.ProcessedCount);
        Assert.Equal(200, report.Topics.Single(t => t.CategoryId == "explanation").Count);
        var signal = Assert.Single(report.Signals);
        Assert.Equal($"f{position}", signal.FeedbackId);
        Assert.Equal("Pending", signal.Status);
        Assert.Contains("назвал меня тупым", signal.Quote);
    }

    [Fact]
    public void CountsUniqueReviewsAndKeepsOpposingOpinionsAndTheirEvidence()
    {
        const string comment = "понятно, ясно, но иногда запутанно";
        var findings = new[] { Finding(), Finding("ясно"), Finding("запутанно", sentiment: "Negative") };
        var report = FeedbackReportBuilder.Build("Т", [new("f1", comment)],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = Analysis("f1", comment, findings) }, [], Categories, "v1");

        var topic = Assert.Single(report.Topics);
        Assert.Equal(1, topic.Count);
        Assert.Equal(1, topic.PositiveCount);
        Assert.Equal(1, topic.NegativeCount);
        Assert.Equal(3, topic.Evidence.Count);
        Assert.All(topic.Evidence, e => Assert.Equal("f1", e.FeedbackId));
    }

    [Fact]
    public void EditedAndDeletedFeedbackCannotSupplyStaleEvidence()
    {
        var report = FeedbackReportBuilder.Build("Т", [new("edited", "теперь другой текст")],
            new Dictionary<string, FeedbackAnalysis>
            {
                ["edited"] = Analysis("edited", "назвал тупым", Finding("назвал тупым", "humiliation", "Negative")),
                ["deleted"] = Analysis("deleted", "назвал тупым", Finding("назвал тупым", "humiliation", "Negative"))
            }, [], Categories, "v1");
        Assert.Empty(report.Signals);
        Assert.Empty(report.Topics);
        Assert.Equal(1, report.PendingCount);
    }

    [Fact]
    public void PartialFailurePreservesSignalButDoesNotCountAsCompleted()
    {
        var analysis = Analysis("f1", "назвал тупым", Finding("назвал тупым", "humiliation", "Negative"));
        analysis.Status = "Failed";
        var report = FeedbackReportBuilder.Build("Т", [new("f1", analysis.SourceComment)],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = analysis }, [], Categories, "v1");
        Assert.Single(report.Signals);
        Assert.Equal(0, report.ProcessedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.PendingCount);
    }

    [Fact]
    public void CatalogueUpdateRetainsEvidenceWhileMarkingAnalysisPending()
    {
        var analysis = Analysis("f1", "понятно", Finding());
        var report = FeedbackReportBuilder.Build("Т", [new("f1", "понятно")],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = analysis }, [], Categories, "v2");
        Assert.Single(report.Topics);
        Assert.Equal(1, report.PendingCount);
        Assert.Equal(0, report.ProcessedCount);
    }

    [Fact]
    public void NewCategoriesRemainProposalsWithEvidence()
    {
        var finding = Finding("нет субтитров");
        finding.CategoryId = null;
        finding.ProposedCategoryName = "Доступность для слабослышащих";
        finding.ProposedCategoryDescription = "Субтитры и альтернативные форматы.";
        FeedbackAnalysisModel.Validate([finding], finding.Quote, Categories, false);
        var report = FeedbackReportBuilder.Build("Т", [new("f1", finding.Quote)],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = Analysis("f1", finding.Quote, finding) }, [], Categories, "v1");
        Assert.Empty(report.Topics);
        Assert.Equal("f1", Assert.Single(Assert.Single(report.ProposedCategories).Evidence).FeedbackId);
    }

    [Fact]
    public void ValidatorRejectsInventedQuotesUnknownIdsAndMissingCategoryProposals()
    {
        Assert.Throws<InvalidDataException>(() => FeedbackAnalysisModel.Validate([Finding("выдумка")], "понятно", Categories, false));
        Assert.Throws<InvalidDataException>(() => FeedbackAnalysisModel.Validate([Finding(category: "unknown")], "понятно", Categories, false));
        var finding = Finding();
        finding.CategoryId = null;
        Assert.Throws<InvalidDataException>(() => FeedbackAnalysisModel.Validate([finding], "понятно", Categories, false));
    }

    [Fact]
    public void KnownSensitiveCategoryForcesAttentionAndEquivalentProposalUsesExistingId()
    {
        var finding = Finding("назвал тупым", "humiliation", "Negative");
        finding.RequiresAttention = false;
        FeedbackAnalysisModel.Validate([finding], finding.Quote, Categories, false);
        Assert.True(finding.RequiresAttention);

        var proposal = Finding();
        proposal.CategoryId = null;
        proposal.ProposedCategoryName = "  понятность   объяснений  ";
        proposal.ProposedCategoryDescription = "Описание";
        FeedbackAnalysisModel.Validate([proposal], proposal.Quote, Categories, false);
        Assert.Equal("explanation", proposal.CategoryId);
        Assert.Null(proposal.ProposedCategoryName);
    }

    [Fact]
    public void SplittingLongCommentPreservesTailAndEveryCharacter()
    {
        var comment = new string('а', 13000) + "важный сигнал в конце";
        var chunks = FeedbackAnalysisModel.SplitComment(comment).ToList();
        Assert.All(chunks, c => Assert.InRange(c.Length, 1, 6000));
        Assert.EndsWith("важный сигнал в конце", chunks.Last());
        var reconstructed = chunks[0] + string.Concat(chunks.Skip(1).Select(c => c[500..]));
        Assert.Equal(comment, reconstructed);
    }

    [Fact]
    public async Task ModelReceivesCatalogueAndUntrustedTextAsDataAndValidatesJson()
    {
        var chat = new FakeChatCompletion { Respond = _ => JsonSerializer.Serialize(new { findings = new[] { Finding() } }, FeedbackAnalysisModel.JsonOptions) };
        var model = new FeedbackAnalysisModel(chat.CreateKernel());
        var result = await model.ExtractAsync("понятно", Categories, false, default);
        Assert.Single(result);
        using var input = JsonDocument.Parse(chat.Requests.Single().Last().Content!);
        Assert.Equal("понятно", input.RootElement.GetProperty("comment").GetString());
        Assert.Equal(Categories.Count, input.RootElement.GetProperty("categories").GetArrayLength());
        chat.Respond = _ => "не JSON";
        await Assert.ThrowsAnyAsync<JsonException>(() => model.ExtractAsync("понятно", Categories, false, default));
    }

    [Fact]
    public void ReviewedStatusIsSeparateFromModelFindingAndResetsOnSourceEdit()
    {
        var finding = Finding("назвал тупым", "humiliation", "Negative");
        var analysis = Analysis("f1", finding.Quote, finding);
        var key = FeedbackAnalysisModel.SignalKey(analysis.SourceComment, finding);
        var review = new FeedbackSignalReview { FeedbackId = "f1", SignalKey = key, Status = "Reviewed", Note = "Результат проверки" };
        var report = FeedbackReportBuilder.Build("Т", [new("f1", analysis.SourceComment)],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = analysis }, [review], Categories, "v1");
        Assert.Equal("Reviewed", Assert.Single(report.Signals).Status);
        Assert.NotEqual(key, FeedbackAnalysisModel.SignalKey("изменённый исходник", finding));
    }

    [Fact]
    public void ApprovedProposalBecomesTopicWithoutLosingHumanReview()
    {
        var finding = Finding("назвал тупым");
        finding.CategoryId = null;
        finding.ProposedCategoryName = "Новая категория";
        finding.ProposedCategoryDescription = "Описание";
        finding.RequiresAttention = true;
        var categories = Categories;
        categories.Add(new("new", "Новая категория", "Описание", true));
        var key = FeedbackAnalysisModel.SignalKey(finding.Quote, finding);
        var report = FeedbackReportBuilder.Build("Т", [new("f1", finding.Quote)],
            new Dictionary<string, FeedbackAnalysis> { ["f1"] = Analysis("f1", finding.Quote, finding) },
            [new() { FeedbackId = "f1", SignalKey = key, Status = "InReview" }], categories, "v2");
        Assert.Empty(report.ProposedCategories);
        Assert.Equal("new", Assert.Single(report.Topics).CategoryId);
        Assert.Equal("InReview", Assert.Single(report.Signals).Status);
    }

    [Fact]
    public void MigrationSnapshotMatchesCurrentModel()
    {
        using var context = new Infrastructure.DataManager.AppDbContextFactory().CreateDbContext([]);
        Assert.False(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.HasPendingModelChanges(context.Database));
    }
}
