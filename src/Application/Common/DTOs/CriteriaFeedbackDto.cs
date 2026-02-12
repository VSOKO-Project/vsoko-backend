using Domain.Entities;

namespace Application.Common.DTOs;

public class CriteriaFeedbackDto
{
    public CriteriaDto? Criteria { get; set; }
    public int Score { get; set; }
}