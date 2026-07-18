namespace HirePathAI.Domain.Constants;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Recruiter = "Recruiter";
    public const string Candidate = "Candidate";
    public const string HiringManager = "HiringManager";

    public static readonly string[] All =
    [
        Admin,
        Recruiter,
        Candidate,
        HiringManager
    ];
}