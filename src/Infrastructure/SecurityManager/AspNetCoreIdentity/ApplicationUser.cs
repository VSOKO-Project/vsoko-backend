using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public class ApplicationUser : IdentityUser
{
    public required string Name { get; set; } = null!;
    public required string Surname { get; set; } = null!;
    public string? Patronymic { get; set; }
    public required UserType Type { get; set; }
    public bool? IsBlocked { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }

    public Student? StudentRef { get; set; }
    public Employee? EmployeeRef { get; set; }
    public List<Refresh>? Refreshes { get; set; }
}
