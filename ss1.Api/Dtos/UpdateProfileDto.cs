namespace ss1.Api.Dtos
{
    public record UpdateProfileDto(
        string? FirstName,
        string? LastName,
        string? PhoneNumber
    );
}