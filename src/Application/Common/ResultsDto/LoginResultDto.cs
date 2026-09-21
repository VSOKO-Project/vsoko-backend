namespace Application.Common.ResultsDto;

public class LoginResultDto
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? Expires { get; init; }
    public string? UserId { get; init; }
    public bool IsAdmin { get; init; }
    public bool MustChangePassword { get; init; }
}
