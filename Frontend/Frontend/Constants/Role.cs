namespace Frontend.Constants;

public static class Role
{
    public const string Admin = nameof(Admin);
    public const string Manager = nameof(Manager);
    public const string Censor = nameof(Censor);
    public const string Employer = nameof(Employer);
    public const string Candidate = nameof(Candidate);

    public static readonly string[] AllRoles = [Admin, Manager, Censor, Employer, Candidate];

    public static string GetDisplayName(string role) => role switch
    {
        Censor => "Kiểm duyệt viên",
        Employer => "Nhà tuyển dụng",
        Candidate => "Ứng viên",
        Manager => "Quản lý",
        Admin => "Quản trị viên",
        _ => role
    };
}
