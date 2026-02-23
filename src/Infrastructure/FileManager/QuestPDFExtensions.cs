using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.FileManager;
public static class PdfExtensions
{
    public static IContainer TableCell(this IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .AlignCenter();
    }

    public static IContainer TableHeader(this IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten4)
            .BorderBottom(2)
            .BorderColor(Colors.Black)
            .PaddingVertical(5)
            .AlignCenter();
    }
}