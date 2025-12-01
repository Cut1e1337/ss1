namespace ss1.Api.Dtos
{
    public record CreateProfileDto(
        string Email,
        string Password,
        string? FirstName,
        string? LastName,
        string? PhoneNumber
    );
}
