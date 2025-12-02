using System.Text.RegularExpressions;
using ss1.Api.Dtos;

namespace ss1.Api.Validation
{
    public static class UpdateProfileValidator
    {
        public static List<string> Validate(UpdateProfileDto dto)
        {
            var errors = new List<string>();

            // FirstName
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

            // PhoneNumber
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
