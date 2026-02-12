namespace Application.Common.DTOs;

public class WorkloadDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Teacher { get; set; }
    public string? TeacherSurname { get; set; } 
    public string? TeacherName { get; set; }
    public string? TeacherPatronymic { get; set; }
    public string TeacherFullName => 
        $"{TeacherSurname} {TeacherName} {TeacherPatronymic}".Trim();
}
