namespace Domain.Shared.Constants;

public static class Role
{
    public const string Admin = nameof(Admin);
    public const string Manager = nameof(Manager);
    public const string Censor = nameof(Censor);
    public const string SupportStaff = nameof(SupportStaff);
    public const string Employer = nameof(Employer);
    public const string Candidate = nameof(Candidate);

    public static readonly string[] AllRoles = [Admin, Manager, Censor, SupportStaff, Employer, Candidate];
}