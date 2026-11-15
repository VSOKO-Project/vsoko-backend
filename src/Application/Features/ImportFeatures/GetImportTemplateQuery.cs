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
                []
            ),
            new SpreadsheetSheet(
                ImportFormat.WorkloadsSheet,
                ImportFormat.WorkloadColumns,
                []
            ),
            new SpreadsheetSheet("Инструкция", ["Как заполнить файл"], [
                ["Заполните лист «Студенты» и/или «Нагрузка». Примеры ниже не импортируются."],
                ["Студенты: ИВТ-21 | Иванов | Пётр | Сергеевич | 210001"],
                ["Нагрузка: Петрова Анна Ивановна | Математический анализ | ИВТ-21"],
                ["Номер зачётки храните как текст, чтобы сохранить ведущие нули."],
                ["Для исправления преподавателей и дисциплин скачайте данные семестра: сохраните колонки ID."],
                ["Для новых строк оставляйте ID пустыми. Переименования действуют во всех семестрах."],
                ["Удаляются только строки нагрузки в режиме синхронизации по выгрузке того же семестра. Студенты не удаляются."],
            ]),
        ]);

        return Task.FromResult(file);
    }
}
