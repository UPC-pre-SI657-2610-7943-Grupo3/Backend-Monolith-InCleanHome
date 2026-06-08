namespace InCleanHome.API.IAM.Domain.Model.Commands;

public record UploadWorkerDocumentCommand(int UserId, string DocumentType, string FileName, string FileBase64);
