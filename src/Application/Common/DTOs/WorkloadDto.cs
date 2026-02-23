namespace Application.Common.DTOs;

public class WorkloadDto
{
    public string? Id { get; set; }
    public TeacherDto? Teacher { get; set; }
    public DisciplineDto? Discipline { get; set; }
    public StudentGroupDto? Group { get; set; }
}
