using Repository.Constants;
using Repository.Models.Address;
using Repository.Models.Users;

namespace Repository.Models.Jobs;

public class Job
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int? VacancyCount { get; set; }
    public Gender? GenderRequirement { get; set; }
    public int? MinAgeRequirement { get; set; }
    public int? MaxAgeRequirement { get; set; }
    public required string CommuneCode { get; set; }
    public required string DetailAddress { get; set; }
    public DateOnly ExpiryAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid EmployerId { get; set; }
    public Guid IndustryId { get; set; }

    public Commune? Commune { get; set; }
    public EmployerProfile? Employer { get; set; }
    public Industry? Industry { get; set; }
    public ICollection<JobSchedule>? JobSchedules { get; set; }
    public ICollection<JobSalary>? JobSalaries { get; set; }
    public ICollection<JobApplication>? JobApplications { get; set; }
    public ICollection<JobReport>? JobReports { get; set; }
}

public enum JobStatus
{
    Active,
    Closed,
    Hidden
}