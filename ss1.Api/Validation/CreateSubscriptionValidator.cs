using System.Text.RegularExpressions;
using ss1.Api.Dtos;

namespace ss1.Api.Validation
{
    public static class CreateSubscriptionValidator
    {
        public static List<string> Validate(SubscriptionDto dto)
        {
            var errors = new List<string>();

            // UserEmail
            if (string.IsNullOrWhiteSpace(dto.UserEmail))
            {
                errors.Add("UserEmail is required.");
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
                errors.Add("PlanName is required.");
            }
            else if (dto.PlanName.Length > 50)
            {
                errors.Add("PlanName cannot be longer than 50 characters.");
            }

            // StartDate / EndDate: для Create можна дозволити default,
            // бо контролер сам ставить дефолти. Якщо обидва вказані —
            // перевіримо, щоб EndDate > StartDate.
            if (dto.StartDate != default && dto.EndDate != default)
            {
                if (dto.EndDate <= dto.StartDate)
                    errors.Add("EndDate must be greater than StartDate.");
            }

            return errors;
        }
    }
}
