using ss1.Models;

namespace ss1.Api.Dtos
{
    public record PhotoSubmissionDto(
        int Id,
        string FileName,
        string UserEmail,
        string? ServiceType,
        string? Comment,
        int Price,
        DateTime UploadedAt,
        bool IsDelivered,
        int OrderNumber,
        int GlobalOrderId,
        SubmissionStatus Status
    );
}