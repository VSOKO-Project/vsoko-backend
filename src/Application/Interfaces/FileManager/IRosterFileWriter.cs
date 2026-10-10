namespace Application.Interfaces.FileManager;

public record SpreadsheetSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows
);

public interface IRosterFileWriter
{
    /// <summary>
    /// Собирает xlsx, все значения пишутся как текст.
    /// </summary>
    byte[] Write(IReadOnlyList<SpreadsheetSheet> sheets);
}
