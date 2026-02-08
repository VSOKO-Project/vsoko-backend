namespace coo.Application.Common.Results;
public class LoginResultDto
{
    public string? AccessToken { get; init; }
    public DateTime? Expires { get; init; }
    public string? UserId { get; init; }
    public bool IsAdmin { get; init; }
}