namespace Application.Common.DTOs;

public record ReportHistoryDto(string Id, string PeriodTitle, DateTime CreatedAtUtc, string FileName, long SizeBytes);
public record ReportFileDto(string FileName, byte[] Content);
