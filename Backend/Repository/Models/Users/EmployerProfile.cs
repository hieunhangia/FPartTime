using Repository.Models.Jobs;

namespace Repository.Models.Users;

public class EmployerProfile
{
    public Guid UserId { get; set; }
    public required int MaxActivePosts { get; set; }
    public string? LogoFilePath { get; set; }
    public string? Description { get; set; }
    public string? ContactPhone { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public required string VerificationDocumentFilePath { get; set; }

    public User? User { get; set; }
    public ICollection<Job>? Jobs { get; set; }
}