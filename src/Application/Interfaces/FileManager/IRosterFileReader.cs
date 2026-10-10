namespace Application.Interfaces.FileManager;

/// <summary>Строка листа: номер строки в файле и значения ячеек в порядке заголовков.</summary>
public record RosterRow(int RowNumber, IReadOnlyList<string> Values);

/// <summary>Лист файла: первая строка — заголовки, дальше непустые строки.</summary>
public record RosterSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<RosterRow> Rows);

public interface IRosterFileReader
{
    /// <summary>
    /// Читает все листы xlsx. Если файл не читается, бросает ValidationException.
    /// </summary>
    IReadOnlyList<RosterSheet> Read(Stream stream);
}
