namespace Application.Common.DTOs;

public class RatingDto
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public int FeedbackCount { get; set; }
    public float Grade { get; set; }
}
