using System.Text.RegularExpressions;
using ss1.Api.Dtos;

namespace ss1.Api.Validation
{
    public static class UpdateSubscriptionValidator
    {
        public static List<string> Validate(SubscriptionDto dto)
        {
            var errors = new List<string>();

            // Id
            if (dto.Id is null || dto.Id <= 0)
                errors.Add("Id must be a positive value for update.");

            // UserEmail
            if (string.IsNullOrWhiteSpace(dto.UserEmail))
            {
                errors.Add("UserEmail cannot be empty.");
            }
            else
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(dto.UserEmail, emailRegex))
                    errors.Add("UserEmail must be a valid email address.");
            }

            // PlanName
            if (string.IsNullOrWhiteSpace(dto.PlanName))
            {
                errors.Add("PlanName cannot be empty.");
            }
            else if (dto.PlanName.Length > 50)
            {
                errors.Add("PlanName cannot be longer than 50 characters.");
            }

            // StartDate / EndDate обовʼязкові для оновлення
            if (dto.StartDate == default)
                errors.Add("StartDate is required.");

            if (dto.EndDate == default)
                errors.Add("EndDate is required.");

            if (dto.StartDate != default && dto.EndDate != default &&
                dto.EndDate <= dto.StartDate)
            {
                errors.Add("EndDate must be greater than StartDate.");
            }

            return errors;
        }
    }
}
