using System.Text.RegularExpressions;
using ss1.Api.Dtos;

namespace ss1.Api.Validation
{
    public static class UpdatePhotoValidator
    {
        public static List<string> Validate(PhotoDto dto)
        {
            var errors = new List<string>();

            // Id (для оновлення має бути валідний)
            if (dto.Id <= 0)
                errors.Add("Id must be a positive value for update.");

            // FileName
            if (string.IsNullOrWhiteSpace(dto.FileName))
                errors.Add("FileName cannot be empty.");

            if (!string.IsNullOrWhiteSpace(dto.FileName) && dto.FileName.Length > 255)
                errors.Add("FileName cannot be longer than 255 characters.");

            // FilePath
            if (string.IsNullOrWhiteSpace(dto.FilePath))
                errors.Add("FilePath cannot be empty.");

            if (!string.IsNullOrWhiteSpace(dto.FilePath) && dto.FilePath.Length > 500)
                errors.Add("FilePath cannot be longer than 500 characters.");

            // UploadDate
            if (dto.UploadDate == default)
                errors.Add("UploadDate must be a valid date.");

            // UserEmail
            if (string.IsNullOrWhiteSpace(dto.UserEmail))
            {
                errors.Add("UserEmail cannot be empty.");
            }
            else
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(dto.UserEmail, emailRegex))
                    errors.Add("UserEmail must be a valid email.");
            }

            // OrderNumber
            if (dto.OrderNumber < 0)
                errors.Add("OrderNumber cannot be negative.");

            return errors;
        }
    }
}
