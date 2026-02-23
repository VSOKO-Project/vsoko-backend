namespace Application.Common.DTOs;

public class TeacherDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Patronymic { get; set; } = null!;
    public string FullName => $"{Surname} {Name} {Patronymic}".Trim();
}
