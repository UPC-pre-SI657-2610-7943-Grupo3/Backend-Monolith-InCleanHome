namespace InCleanHome.API.IAM.Domain.Model.Commands;

public record VerifyUserCommand(int UserId);
public record ApproveWorkerDocumentsCommand(int UserId);
public record RejectWorkerDocumentsCommand(int UserId);
public record SuspendUserCommand(int UserId, TimeSpan Duration, string Reason);
public record ClearUserSuspensionCommand(int UserId);
