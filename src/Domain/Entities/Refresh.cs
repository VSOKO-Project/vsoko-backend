using Domain.Common;

namespace Domain.Entities;

public class Refresh : BaseEntity
{
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    // Null on legacy sessions: they must authenticate again after upgrading.
    public string? SecurityStamp { get; set; }
    public DateTime ExpiresAt { get; set; }
}
