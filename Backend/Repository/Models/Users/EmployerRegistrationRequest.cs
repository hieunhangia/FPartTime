namespace Repository.Models.Users;

public class EmployerRegistrationRequest
{
    public Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public required string TaxCode { get; set; }
    public required string DocumentPath { get; set; }
    public required string FullName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CCCDNumber { get; set; }
    public required string CCCDFrontImagePath { get; set; } 
    public required string CCCDBackImagePath { get; set; } 
    public string? RejectReason { get; set; }
    public EmployerRegistrationRequestStatus Status { get; set; } = EmployerRegistrationRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? ProcessorId { get; set; }

    public CandidateProfile? Requester { get; set; }
    public CensorProfile? Processor { get; set; }
}

public enum EmployerRegistrationRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}