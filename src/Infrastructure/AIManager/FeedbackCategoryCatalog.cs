using Domain.Entities;

namespace Infrastructure.AIManager;

public static class FeedbackCategoryCatalog
{
    public static string Normalize(string value) => string.Join(' ', value.Trim().Split((char[]?)null,
        StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    public static FeedbackCategory[] Defaults =>
    [
        Category("explanation", "Понятность объяснений", "Ясность изложения, ответы на вопросы и объяснение сложных тем."),
        Category("assessment", "Прозрачность оценивания", "Понятность, стабильность и справедливость критериев и выставления оценок."),
        Category("materials", "Учебные материалы", "Качество, актуальность и доступность материалов и методичек."),
        Category("practice", "Практика и содержание", "Практические задания, содержание курса, соотношение теории и практики."),
        Category("workload", "Нагрузка и организация", "Объём заданий, сроки, расписание и организация занятий."),
        Category("communication", "Общение и поддержка", "Доступность преподавателя, обратная связь и уважительное общение."),
        Category("humiliation", "Возможные унижения и травля", "Сообщения об оскорблениях, публичных унижениях, систематическом преследовании.", true),
        Category("threats", "Возможные угрозы и давление", "Угрозы, запугивание, месть за жалобы, принуждение.", true),
        Category("discrimination", "Возможная дискриминация", "Неравное обращение по личным или групповым признакам.", true),
        Category("harassment", "Возможные домогательства", "Нежелательные сексуальные комментарии, предложения или действия.", true),
        Category("corruption", "Возможные неправомерные требования", "Требования денег, подарков или услуг в обмен на оценки.", true),
        Category("other-risk", "Другая ситуация, требующая проверки", "Иные сообщения о возможном вреде или серьёзном нарушении, не подходящие к остальным категориям.", true)
    ];

    private static FeedbackCategory Category(string id, string name, string description, bool attention = false) =>
        new() { Id = id, Name = name, NormalizedName = Normalize(name), Description = description, RequiresAttention = attention };
}
