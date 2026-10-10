using System.Globalization;
using Application.Common.Exceptions;
using Application.Interfaces.FileManager;
using ClosedXML.Excel;

namespace Infrastructure.FileManager;

/// <summary>
/// Чтение и запись xlsx на ClosedXML.
/// </summary>
public class RosterFile : IRosterFileReader, IRosterFileWriter
{
    private const int MaxSheetNameLength = 31;
    private static readonly char[] InvalidSheetNameChars = ['[', ']', ':', '*', '?', '/', '\\'];

    public IReadOnlyList<RosterSheet> Read(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            throw new ValidationException("Не удалось прочитать файл. Сохраните его в формате .xlsx");
        }

        using (workbook)
        {
            return workbook.Worksheets.Select(ReadSheet).ToList();
        }
    }

    private static RosterSheet ReadSheet(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
            return new RosterSheet(worksheet.Name, [], []);

        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var headers = Enumerable.Range(1, lastColumn)
            .Select(c => CellText(headerRow.Cell(c)))
            .ToList();

        var rows = worksheet.RowsUsed()
            .Where(r => r.RowNumber() > headerRow.RowNumber())
            .Select(r => new RosterRow(
                r.RowNumber(),
                Enumerable.Range(1, lastColumn).Select(c => CellText(r.Cell(c))).ToList()
            ))
            .ToList();

        return new RosterSheet(worksheet.Name, headers, rows);
    }

    private static string CellText(IXLCell cell)
    {
        // Номер зачётки часто хранится числом: 210001, а не 210001.0.
        if (cell.DataType == XLDataType.Number)
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);

        return cell.GetFormattedString();
    }

    public byte[] Write(IReadOnlyList<SpreadsheetSheet> sheets)
    {
        using var workbook = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in sheets)
        {
            var worksheet = workbook.Worksheets.Add(UniqueSheetName(sheet.Name, usedNames));
            var columnCount = Math.Max(sheet.Headers.Count, 1);

            // Текстовый формат на всю колонку, чтобы номера зачёток не превращались в числа.
            worksheet.Columns(1, columnCount).Style.NumberFormat.Format = "@";

            for (var c = 0; c < sheet.Headers.Count; c++)
            {
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = sheet.Headers[c];
                cell.Style.Font.Bold = true;
            }

            for (var r = 0; r < sheet.Rows.Count; r++)
            {
                for (var c = 0; c < sheet.Rows[r].Count; c++)
                {
                    worksheet.Cell(r + 2, c + 1).Value = sheet.Rows[r][c];
                }
            }

            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns(1, columnCount).AdjustToContents();
        }

        if (workbook.Worksheets.Count == 0)
            workbook.Worksheets.Add("Нет данных");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string UniqueSheetName(string name, HashSet<string> usedNames)
    {
        var clean = new string(name.Select(ch => InvalidSheetNameChars.Contains(ch) ? '_' : ch).ToArray()).Trim('\'').Trim();
        if (clean.Length == 0)
            clean = "Лист";
        if (clean.Length > MaxSheetNameLength)
            clean = clean[..MaxSheetNameLength];

        var candidate = clean;
        for (var i = 2; !usedNames.Add(candidate); i++)
        {
            var suffix = $" ({i})";
            candidate = clean[..Math.Min(clean.Length, MaxSheetNameLength - suffix.Length)] + suffix;
        }

        return candidate;
    }
}
