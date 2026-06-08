namespace InCleanHome.API.IAM.Domain.Model.Commands;

public record UpdateUserEmailCommand(int UserId, string Email);

