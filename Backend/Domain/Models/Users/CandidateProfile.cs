using Domain.Models.Address;
using Domain.Models.Jobs;
using Domain.Shared.Enums;

namespace Domain.Models.Users;

public class CandidateProfile
{
    public Guid UserId { get; set; }
    public string? AvatarFilePath { get; set; }
    public string? Bio { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? PreferredWorkCommuneCode { get; set; }
    public DateTime? OpenForJobUntil { get; set; }

    public User? User { get; set; }
    public Commune? PreferredWorkCommune { get; set; }
    public ICollection<CandidateSchedule>? PreferredWorkSchedules { get; set; }
    public ICollection<EmployerRegistrationRequest>? EmployerRegistrationRequests { get; set; }
    public ICollection<JobApplication>? JobApplications { get; set; }
    public ICollection<JobReport>? JobReports { get; set; }
}