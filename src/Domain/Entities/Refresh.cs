using Domain.Common;

namespace Domain.Entities;

public class Refresh : BaseEntity
{
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
