namespace Application.Common.Interfaces;

public interface IUserContext
{
    string? UserName { get; }
    string? UserId { get; }
    string? Role { get; }
    string? StudentGroup { get; }
}
