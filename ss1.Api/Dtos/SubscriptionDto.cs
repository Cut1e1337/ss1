using System;

namespace ss1.Api.Dtos
{
    public class SubscriptionDto
    {
        public int? Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string PlanName { get; set; } = "Basic";

        public bool AutoRenew { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
