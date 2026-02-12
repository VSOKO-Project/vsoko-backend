using Domain.Enums;

namespace Application.Common.DTOs;

public class CriteriaDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public CriteriaObject Object { get; set; }
}
