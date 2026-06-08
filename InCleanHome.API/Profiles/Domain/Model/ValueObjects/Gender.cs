namespace InCleanHome.API.Profiles.Domain.Model.ValueObjects;

public static class Gender
{
    public const string Female = "female";
    public const string Male   = "male";
    public const string Other  = "other";

    public static bool IsValid(string g) => g is Female or Male or Other;
}
