using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Seeders;

public static class DataSeeder
{
    /// <summary>
    /// Заполняет базу данных тестовыми данными.
    /// Безопасно вызывать повторно — проверяет наличие данных перед вставкой.
    /// </summary>
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        if (await db.Teachers.AnyAsync())
            return; // данные уже засеяны

        // ──────────────── Роли сотрудников ────────────────
        var roles = new List<EmployeeRole>
        {
            new() { Name = "Admin" },
            new() { Name = "Методист" },
            new() { Name = "Заведующий кафедрой" },
        };
        db.EmployeeRoles.AddRange(roles);
        await db.SaveChangesAsync();

        // ──────────────── Преподаватели ────────────────
        var teachers = new List<Teacher>
        {
            new() { Name = "Иванов",    Surname = "Алексей",   Patronymic = "Петрович" },
            new() { Name = "Петрова",   Surname = "Мария",     Patronymic = "Ивановна" },
            new() { Name = "Сидоров",   Surname = "Дмитрий",   Patronymic = "Сергеевич" },
            new() { Name = "Козлова",   Surname = "Анна",      Patronymic = "Викторовна" },
            new() { Name = "Морозов",   Surname = "Николай",   Patronymic = "Андреевич" },
            new() { Name = "Волкова",   Surname = "Елена",     Patronymic = "Дмитриевна" },
            new() { Name = "Новиков",   Surname = "Артём",     Patronymic = "Олегович" },
            new() { Name = "Кузнецова", Surname = "Ольга",     Patronymic = "Александровна" },
        };
        db.Teachers.AddRange(teachers);
        await db.SaveChangesAsync();

        // ──────────────── Дисциплины ────────────────
        var disciplines = new List<Discipline>
        {
            new() { Name = "Математический анализ" },
            new() { Name = "Линейная алгебра" },
            new() { Name = "Физика" },
            new() { Name = "Программирование" },
            new() { Name = "Базы данных" },
            new() { Name = "Операционные системы" },
            new() { Name = "Иностранный язык" },
            new() { Name = "Философия" },
            new() { Name = "История" },
            new() { Name = "Дискретная математика" },
        };
        db.Disciplines.AddRange(disciplines);
        await db.SaveChangesAsync();

        // ──────────────── Группы студентов ────────────────
        var groups = new List<StudentGroup>
        {
            new() { Name = "ИС-21",   Semester = 6 },
            new() { Name = "ИС-22",   Semester = 4 },
            new() { Name = "ИС-23",   Semester = 2 },
            new() { Name = "ПМ-21",   Semester = 6 },
            new() { Name = "ПМ-22",   Semester = 4 },
            new() { Name = "ИВТ-21",  Semester = 6 },
        };
        db.StudentGroups.AddRange(groups);
        await db.SaveChangesAsync();

        // ──────────────── Студенты + пользователи ────────────────
        var firstNames = new[]
        {
            "Александр", "Дарья", "Максим", "Екатерина", "Артём",
            "Виктория", "Никита", "Анастасия", "Кирилл", "Полина",
            "Михаил", "Софья", "Даниил", "Алиса", "Илья",
            "Ксения", "Роман", "Мария", "Тимур", "Елизавета",
            "Егор", "Вероника", "Иван", "Арина",
        };
        var lastNames = new[]
        {
            "Смирнов", "Кузнецов", "Попов", "Васильев", "Соколов",
            "Михайлов", "Новиков", "Фёдоров", "Морозов", "Волков",
            "Алексеев", "Лебедев", "Семёнов", "Егоров", "Павлов",
            "Козлов", "Степанов", "Николаев", "Орлов", "Андреев",
            "Макаров", "Никитин", "Захаров", "Зайцев",
        };

        var students = new List<Student>();
        int nameIndex = 0;

        foreach (var group in groups)
        {
            int studentsPerGroup = 4; // 4 студента на группу = 24 студента
            for (int i = 0; i < studentsPerGroup; i++)
            {
                var idx = nameIndex % firstNames.Length;
                var studentId = Guid.NewGuid().ToString();

                // Сначала создаём пользователя через UserManager
                var user = new ApplicationUser
                {
                    Id = studentId,
                    UserName = $"student{nameIndex + 1}",
                    Name = firstNames[idx],
                    Surname = lastNames[idx],
                    Type = UserType.Student,
                    CreatedAt = DateTime.UtcNow,
                };
                await userManager.CreateAsync(user, "Student123!");

                // Затем создаём студента с тем же Id
                var student = new Student
                {
                    Id = studentId,
                    GroupId = group.Id,
                };
                students.Add(student);
                nameIndex++;
            }
        }

        db.Students.AddRange(students);
        await db.SaveChangesAsync();

        // ──────────────── Нагрузка (Workload) ────────────────
        // Каждый преподаватель ведёт 2-3 дисциплины в разных группах
        var workloads = new List<Workload>();

        void AddWorkload(int teacherIdx, int disciplineIdx, int groupIdx)
        {
            workloads.Add(new Workload
            {
                TeacherRef = teachers[teacherIdx],
                DisciplineRef = disciplines[disciplineIdx],
                GroupRef = groups[groupIdx],
            });
        }

        // Иванов — Мат.анализ для ИС-21 и ПМ-21
        AddWorkload(0, 0, 0);
        AddWorkload(0, 0, 3);
        // Петрова — Линейная алгебра для ИС-22 и ПМ-22
        AddWorkload(1, 1, 1);
        AddWorkload(1, 1, 4);
        // Сидоров — Физика для ИС-21 и ИВТ-21
        AddWorkload(2, 2, 0);
        AddWorkload(2, 2, 5);
        // Козлова — Программирование для ИС-23 и ПМ-22
        AddWorkload(3, 3, 2);
        AddWorkload(3, 3, 4);
        // Морозов — Базы данных для ИС-22 и ИВТ-21
        AddWorkload(4, 4, 1);
        AddWorkload(4, 4, 5);
        // Волкова — ОС для ПМ-21 и ИС-23
        AddWorkload(5, 5, 3);
        AddWorkload(5, 5, 2);
        // Новиков — Иностранный язык для ИС-21 и ПМ-21
        AddWorkload(6, 6, 0);
        AddWorkload(6, 6, 3);
        // Кузнецова — Философия для ИС-22, История для ПМ-22
        AddWorkload(7, 7, 1);
        AddWorkload(7, 8, 4);

        db.Workloads.AddRange(workloads);
        await db.SaveChangesAsync();

        // ──────────────── Критерии оценки ────────────────
        var criterias = new List<Criteria>
        {
            // Критерии для преподавателя
            new() { Name = "Доступность изложения материала",            Object = CriteriaObject.Teacher },
            new() { Name = "Умение заинтересовать предметом",            Object = CriteriaObject.Teacher },
            new() { Name = "Объективность оценивания",                   Object = CriteriaObject.Teacher },
            new() { Name = "Доброжелательность и уважение к студентам",  Object = CriteriaObject.Teacher },
            new() { Name = "Пунктуальность",                             Object = CriteriaObject.Teacher },
            // Критерии для дисциплины
            new() { Name = "Актуальность содержания курса",              Object = CriteriaObject.Discipline },
            new() { Name = "Качество учебных материалов",                Object = CriteriaObject.Discipline },
            new() { Name = "Практическая применимость знаний",           Object = CriteriaObject.Discipline },
            new() { Name = "Логичность структуры курса",                 Object = CriteriaObject.Discipline },
        };
        db.Criterias.AddRange(criterias);
        await db.SaveChangesAsync();

        // ──────────────── Отзывы (Feedback + CriteriaFeedback) ────────────────
        var rng = new Random(42); // фиксированный seed для воспроизводимости
        var comments = new[]
        {
            "Отличный курс, всё понятно!",
            "Хороший преподаватель, рекомендую.",
            "Материал интересный, но сложный.",
            "Много практических заданий, это плюс.",
            "Всё на высшем уровне.",
            "Есть над чем поработать, но в целом хорошо.",
            "Нормально, без нареканий.",
            "Очень полезные знания для будущей работы.",
            "Лекции интересные, семинары — ещё лучше.",
            "Сложно, но преподаватель помогает разобраться.",
        };

        var teacherCriterias = criterias.Where(c => c.Object == CriteriaObject.Teacher).ToList();
        var disciplineCriterias = criterias.Where(c => c.Object == CriteriaObject.Discipline).ToList();

        foreach (var workload in workloads)
        {
            // Найти студентов этой группы
            var groupStudents = students.Where(s => s.GroupId == workload.GroupId).ToList();

            foreach (var student in groupStudents)
            {
                // ~70% студентов оставляют отзыв
                if (rng.NextDouble() > 0.7)
                    continue;

                var feedback = new Feedback
                {
                    StudentRef = student,
                    WorkloadRef = workload,
                    Comment = comments[rng.Next(comments.Length)],
                };
                db.Feedbacks.Add(feedback);
                await db.SaveChangesAsync();

                // Оценки по критериям преподавателя (1-5)
                foreach (var c in teacherCriterias)
                {
                    db.criteriaFeedbacks.Add(new CriteriaFeedback
                    {
                        CriteriaRef = c,
                        FeedbackRef = feedback,
                        CriteriaScore = rng.Next(3, 6), // 3-5 — реалистичный разброс
                    });
                }

                // Оценки по критериям дисциплины (1-5)
                foreach (var c in disciplineCriterias)
                {
                    db.criteriaFeedbacks.Add(new CriteriaFeedback
                    {
                        CriteriaRef = c,
                        FeedbackRef = feedback,
                        CriteriaScore = rng.Next(2, 6), // 2-5
                    });
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
