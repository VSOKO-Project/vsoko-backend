namespace Application.Common.DTOs;

public class StudentDto
{
    public string Id { get; set; } = null!;
    public StudentGroupDto? Group { get; set; }
}
