using Application.Common.Periods;

namespace Application.Features.ImportFeatures;

/// <summary>
/// Формат файла импорта: названия листов и колонок, лимиты.
/// </summary>
public static class ImportFormat
{
    public const string StudentsSheet = "Студенты";
    public const string WorkloadsSheet = "Нагрузка";

    public const string Group = "Группа";
    public const string Surname = "Фамилия";
    public const string Name = "Имя";
    public const string Patronymic = "Отчество";
    public const string StudentNumber = "Номер зачётки";

    public const string TeacherFio = "ФИО преподавателя";
    public const string Discipline = "Дисциплина";

    /// <summary>
    /// Скрытый лист выгрузки: из какого семестра и по каким преподавателям она сделана.
    /// По нему импорт понимает, какую нагрузку удалить, если строки убрали из файла.
    /// </summary>
    public const string ScopeSheet = "_vsoko";
    public const string ScopeKey = "Ключ";
    public const string ScopeValue = "Значение";
    public const string ScopePeriod = "period";
    public const string ScopeTeacher = "teacher";

    public const long MaxFileSize = 5 * 1024 * 1024;
    public const int MaxRowsPerSheet = 10_000;

    public static readonly string[] StudentColumns = [Group, Surname, Name, Patronymic, StudentNumber];
    public static readonly string[] WorkloadColumns = [TeacherFio, Discipline, Group];
    public static readonly string[] ScopeColumns = [ScopeKey, ScopeValue];

    public static string PeriodValue(PeriodKey period) => $"{period.StartYear}:{period.Term}";

    /// <summary>Временный пароль студента выводится из номера зачётки.</summary>
    public static string TemporaryPassword(string studentNumber) => $"Vsoko{studentNumber}";
}
