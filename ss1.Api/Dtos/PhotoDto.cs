namespace ss1.Api.Dtos
{
    public record PhotoDto(
        int Id,
        string FileName,
        string FilePath,
        bool IsReviewed,
        DateTime UploadDate,
        string UserEmail,
        int OrderNumber
    );
}