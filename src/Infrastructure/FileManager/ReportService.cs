using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.FileManager;

public class ReportService : IReportService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly string _webRootPath;

    public ReportService(
        ITeacherRepository teacherRepository, 
        IDisciplineRepository disciplineRepository,
        ICriteriaRepository criteriaRepository,
        IWebHostEnvironment env)
    {
        _teacherRepository = teacherRepository;
        _disciplineRepository = disciplineRepository;
        _criteriaRepository = criteriaRepository;
        _webRootPath = env.WebRootPath;
    }

    public async Task<byte[]> GenerateReportAsync(CancellationToken cancellationToken)
    {
        var disciplinesRating = await _disciplineRepository.GetAllRatingAsync(cancellationToken);
        var teacherRating = await _teacherRepository.GetAllRatingAsync(cancellationToken);
        var criteriaRating = await _criteriaRepository.GetAllRatingAsync(cancellationToken);

        var avgTeacher = teacherRating.Where(x => x.Grade > 0).Select(x => x.Grade).DefaultIfEmpty(0).Average();
        var avgDiscipline = disciplinesRating.Where(x => x.Grade > 0).Select(x => x.Grade).DefaultIfEmpty(0).Average();
        var avgCriteria = criteriaRating.Where(x => x.Grade > 0).Select(x => x.Grade).DefaultIfEmpty(0).Average();

        Console.WriteLine(_webRootPath);

        var logoPath = Path.Combine(_webRootPath, "Images", "slogo.png");
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("VSOKO-unn").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text("Система мониторинга качества образования").FontSize(9).Italic();
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().PaddingBottom(20).Background(Colors.Grey.Lighten4).Padding(15).Column(summary =>
                    {
                        summary.Item().Text("КРАТКАЯ СВОДКА").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
                        summary.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("Преподаватели").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text(avgTeacher.ToString("0.00")).FontSize(24).Bold().FontColor(Colors.Blue.Darken1);
                            });

                            row.RelativeItem().Column(c => {
                                c.Item().Text("Дисциплины").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text(avgDiscipline.ToString("0.00")).FontSize(24).Bold().FontColor(Colors.Green.Darken1);
                            });

                            row.RelativeItem().Column(c => {
                                c.Item().Text("Общий индекс").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text(avgCriteria.ToString("0.00")).FontSize(24).Bold().FontColor(Colors.Black);
                            });
                        });
                    });

                    col.Item().Text("Аналитический отчет").FontSize(18).Bold().AlignCenter();
                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    col.Item().EnsureSpace(200).Column(innerCol => 
                    {
                        innerCol.Item().PaddingTop(15).Text("1. Рейтинг преподавательского состава").Bold().FontSize(14);
                        innerCol.Item().Table(t => GenerateRatingTable(t, teacherRating, "ФИО преподавателя", avgTeacher));
                    });

                    col.Item().EnsureSpace(200).Column(innerCol =>
                    {
                        innerCol.Item().PaddingTop(15).Text("2. Рейтинг учебных дисциплин").Bold().FontSize(14);
                        innerCol.Item().Table(t => GenerateRatingTable(t, disciplinesRating, "Название предмета", avgDiscipline));
                    });

                    col.Item().EnsureSpace(200).Column(innerCol =>
                    {
                        innerCol.Item().PaddingTop(15).Text("3. Средние оценки по критериям").Bold().FontSize(14);
                        innerCol.Item().Table(t => GenerateRatingTable(t, criteriaRating, "Название критерия", avgCriteria));
                    });
                });
                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(x =>
                    {
                        x.Span("Сформировано автоматически: ");
                        x.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                    });

                    row.RelativeItem().AlignMiddle().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                    });

                    if (File.Exists(logoPath))
                    {
                        row.ConstantItem(150).AlignMiddle().AlignRight().Image(logoPath);
                    }
                });
            });
        }).GeneratePdf();
    }

    private void GenerateRatingTable(TableDescriptor table, IEnumerable<RatingDto> data, string nameColumnTitle, double average)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(40);
            columns.RelativeColumn();
            columns.ConstantColumn(80); 
        });

        static IContainer HeaderStyle(IContainer container) => container
            .BorderBottom(2).BorderColor(Colors.Black)
            .PaddingVertical(8)
            .AlignCenter();

        static IContainer CellStyle(IContainer container) => container
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(4);

        table.Header(header =>
        {
            header.Cell().Element(HeaderStyle).Text("№").SemiBold();
            header.Cell().Element(HeaderStyle).AlignLeft().Text(nameColumnTitle).SemiBold();
            header.Cell().Element(HeaderStyle).Text("Оценка").SemiBold();
        });

        uint index = 1;
        foreach (var item in data)
        {
            var backgroundColor = (index % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;

            table.Cell().Background(backgroundColor).Element(CellStyle).AlignCenter().Text(index.ToString());
            table.Cell().Background(backgroundColor).Element(CellStyle).AlignLeft().Text(item.Name);
            
            var gradeText = item.Grade == 0 ? "-" : item.Grade.ToString("0.0");
            table.Cell().Background(backgroundColor).Element(CellStyle).AlignCenter().Text(gradeText);
            
            index++;
        }

        table.Cell().ColumnSpan(2).PaddingVertical(10).PaddingHorizontal(5).AlignRight().Text("СРЕДНИЙ БАЛЛ:").SemiBold();
        table.Cell().Background(Colors.Blue.Lighten4).PaddingVertical(10).AlignCenter().Text(average.ToString("0.00")).SemiBold();
    }
}