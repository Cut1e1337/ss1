namespace ss1.Api.Dtos
{
    public record ProfileDto(
        int Id,
        string Email,
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        string Role,
        DateTime RegisteredAt
    );
}