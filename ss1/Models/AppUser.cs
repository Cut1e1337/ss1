using System.ComponentModel.DataAnnotations;

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }


    public bool EmailConfirmed { get; set; }

    public string? EmailConfirmationCode { get; set; }

    [Required]
    public string Role { get; set; } = "User";

    public DateTime RegisteredAt { get; set; } = DateTime.Now;

    // 👇 Нові поля:
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    public byte[]? AvatarImage { get; set; }

}
