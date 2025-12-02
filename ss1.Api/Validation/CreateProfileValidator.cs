using System.Text.RegularExpressions;
using ss1.Api.Dtos;

namespace ss1.Api.Validation
{
    public static class CreateProfileValidator
    {
        public static List<string> Validate(CreateProfileDto dto)
        {
            var errors = new List<string>();

            // Email
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                errors.Add("Email is required.");
            }
            else
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(dto.Email, emailRegex))
                    errors.Add("Email must be a valid email address.");
            }

            // Password (мінімум 6 символів, як приклад)
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                errors.Add("Password is required.");
            }
            else if (dto.Password.Length < 6)
            {
                errors.Add("Password must be at least 6 characters long.");
            }

            // FirstName (опціонально, але якщо вказано — адекватна довжина)
            if (!string.IsNullOrWhiteSpace(dto.FirstName) &&
                (dto.FirstName.Length < 2 || dto.FirstName.Length > 50))
            {
                errors.Add("FirstName must be between 2 and 50 characters.");
            }

            // LastName
            if (!string.IsNullOrWhiteSpace(dto.LastName) &&
                (dto.LastName.Length < 2 || dto.LastName.Length > 50))
            {
                errors.Add("LastName must be between 2 and 50 characters.");
            }

            // PhoneNumber (опціональний, але якщо є — простенька перевірка)
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phoneRegex = @"^[0-9+\-\s]+$";
                if (!Regex.IsMatch(dto.PhoneNumber, phoneRegex))
                    errors.Add("PhoneNumber contains invalid characters.");
            }

            return errors;
        }
    }
}
