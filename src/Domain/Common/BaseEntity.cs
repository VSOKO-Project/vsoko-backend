namespace Domain.Common;

public abstract class BaseEntity : IHasAudit, IHasId
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime? CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
}
