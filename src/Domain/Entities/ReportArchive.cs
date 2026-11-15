namespace Domain.Entities;

public class ReportArchive
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PeriodTitle { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}
