using ss1.Dtos;
using System.Text.RegularExpressions;

namespace ss1.Api.Validation
{
    public static class UpdateAlbumValidator
    {
        public static List<string> Validate(AlbumDto dto)
        {
            var errors = new List<string>();

            // Title
            if (string.IsNullOrWhiteSpace(dto.Title))
                errors.Add("Title cannot be empty.");

            if (!string.IsNullOrWhiteSpace(dto.Title) &&
                (dto.Title.Length < 2 || dto.Title.Length > 100))
                errors.Add("Title must be between 2 and 100 characters.");

            // Description
            if (dto.Description?.Length > 500)
                errors.Add("Description cannot exceed 500 characters.");

            // Email
            if (string.IsNullOrWhiteSpace(dto.OwnerEmail))
                errors.Add("OwnerEmail cannot be empty.");
            else
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(dto.OwnerEmail, emailRegex))
                    errors.Add("OwnerEmail must be a valid email.");
            }

            // CoverUrl (опціональне, як і вище)
            if (!string.IsNullOrWhiteSpace(dto.CoverUrl) &&
                dto.CoverUrl.Length > 500)
            {
                errors.Add("CoverUrl is too long.");
            }

            return errors;
        }
    }
}
