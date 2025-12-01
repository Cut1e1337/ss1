namespace ss1.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsReviewed { get; set; } = false;
        public DateTime UploadDate { get; set; }
        public string UserEmail { get; set; }
        public int OrderNumber { get; set; }

    }
}
