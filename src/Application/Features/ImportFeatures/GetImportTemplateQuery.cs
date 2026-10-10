using Application.Common.CQRS;
using Application.Interfaces.FileManager;
using MediatR;

namespace Application.Features.ImportFeatures;

public record GetImportTemplateQuery : IRequest<byte[]>, IQuery;

public class GetImportTemplateQueryHandler : IRequestHandler<GetImportTemplateQuery, byte[]>
{
    private readonly IRosterFileWriter _writer;

    public GetImportTemplateQueryHandler(IRosterFileWriter writer)
    {
        _writer = writer;
    }

    public Task<byte[]> Handle(GetImportTemplateQuery request, CancellationToken cancellationToken)
    {
        var file = _writer.Write([
            new SpreadsheetSheet(
                ImportFormat.StudentsSheet,
                ImportFormat.StudentColumns,
                [["ИВТ-21", "Иванов", "Пётр", "Сергеевич", "210001"]]
            ),
            new SpreadsheetSheet(
                ImportFormat.WorkloadsSheet,
                ImportFormat.WorkloadColumns,
                [["Петрова Анна Ивановна", "Математический анализ", "ИВТ-21"]]
            ),
        ]);

        return Task.FromResult(file);
    }
}
