namespace ss1.Models
{
    public class StatusUpdateModel
    {
        public int Id { get; set; }
        public string NewStatus { get; set; } = string.Empty; // string, не enum!
    }


}
