using Application.Common.CQRS;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using MediatR;

namespace Application.Features.ImportFeatures;

/// <summary>Выгрузка логинов и временных паролей студентов, по листу на группу.</summary>
public record GetCredentialsQuery(IReadOnlyCollection<string>? GroupIds) : IRequest<byte[]>, IQuery;

public class GetCredentialsQueryHandler : IRequestHandler<GetCredentialsQuery, byte[]>
{
    private static readonly string[] Headers = ["Группа", "ФИО", "Логин", "Временный пароль", "Пароль сменён"];

    private readonly IImportRepository _importRepository;
    private readonly IRosterFileWriter _writer;

    public GetCredentialsQueryHandler(IImportRepository importRepository, IRosterFileWriter writer)
    {
        _importRepository = importRepository;
        _writer = writer;
    }

    public async Task<byte[]> Handle(GetCredentialsQuery request, CancellationToken cancellationToken)
    {
        var groupIds = request.GroupIds is { Count: > 0 } ids ? ids : null;
        var students = await _importRepository.GetStudentCredentialsAsync(groupIds, cancellationToken);

        // Временный пароль действует, пока студент его не сменил.
        var sheets = students
            .GroupBy(s => s.GroupName)
            .OrderBy(g => g.Key)
            .Select(g => new SpreadsheetSheet(
                g.Key,
                Headers,
                g.OrderBy(s => s.FullName)
                    .Select(s => (IReadOnlyList<string>)[
                        s.GroupName,
                        s.FullName,
                        s.Login,
                        s.MustChangePassword ? ImportFormat.TemporaryPassword(s.Login) : "",
                        s.MustChangePassword ? "" : "да",
                    ])
                    .ToList()
            ))
            .ToList();

        return _writer.Write(sheets);
    }
}
