using Domain.Enums;
using FluentValidation;

namespace Application.Features.ImportFeatures;

/// <summary>Поля формы импорта: файл и период.</summary>
public abstract class ImportFileRequest
{
    public Stream? Content { get; init; }
    public string? FileName { get; init; }
    public long Length { get; init; }
    public int StartYear { get; init; }
    public Term Term { get; init; }
}

public class ImportFileRequestValidator<T> : AbstractValidator<T>
    where T : ImportFileRequest
{
    public ImportFileRequestValidator()
    {
        RuleFor(w => w.Content).NotNull().WithMessage("Файл не передан");
        RuleFor(w => w.FileName)
            .Must(name => name is not null && name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Принимается только файл .xlsx");
        RuleFor(w => w.Length)
            .GreaterThan(0).WithMessage("Файл пустой")
            .LessThanOrEqualTo(ImportFormat.MaxFileSize).WithMessage("Файл больше 5 МБ");
        RuleFor(w => w.StartYear).InclusiveBetween(2000, 2100);
        RuleFor(w => w.Term).IsInEnum();
    }
}
