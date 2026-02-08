using coo.Domain.Common;
using coo.Domain.Enums;

namespace coo.Domain.Entities;

public class Criteria : BaseEntity
{
    public string Name { get; set; } = null!;
    public required CriteriaObject Object { get; set; }
    public List<CriteriaFeedback>? CriteriaFeedbackRefs { get; init; }
}