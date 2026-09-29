namespace Repository.Constants;

public static class Role
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Censor = "Censor";
    public const string SupportStaff = "SupportStaff";
    public const string Employer = "Employer";
    public const string Student = "Student";

    public static readonly string[] AllRoles = [Admin, Manager, Censor, SupportStaff, Employer, Student];
    public static readonly string[] StaffRoles = [Admin, Manager, Censor, SupportStaff];
}