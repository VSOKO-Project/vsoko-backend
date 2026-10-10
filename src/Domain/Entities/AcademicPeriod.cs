using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class AcademicPeriod : BaseEntity
{
    // Учебный год идентифицируется годом начала: 2026/27 → 2026, весна 2027 тоже 2026.
    public int StartYear { get; set; }
    public Term Term { get; set; }
    public bool IsFeedbackOpen { get; set; }

    public List<Workload>? WorkloadRefs { get; init; }
}
