using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repository.Constants;
using Repository.Models.Address;
using Repository.Models.Jobs;
using Repository.Models.Notifications;
using Repository.Models.Users;

namespace Repository;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Commune> Communes { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<CandidateProfile> Candidates { get; set; }
    public DbSet<EmployerRegistrationRequest> EmployerRegistrationRequests { get; set; }
    public DbSet<CandidateSchedule> CandidateSchedules { get; set; }
    public DbSet<EmployerProfile> Employers { get; set; }
    public DbSet<Industry> Industries { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<JobSchedule> JobSchedules { get; set; }
    public DbSet<JobSalary> JobSalaries { get; set; }
    public DbSet<JobReport> JobReports { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.Token).HasMaxLength(BusinessRuleConstants.Models.RefreshToken.TokenMaxLength);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Commune>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(BusinessRuleConstants.Models.Commune.CodeMaxLength)
                .ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(BusinessRuleConstants.Models.Commune.NameMaxLength);

            entity.HasOne(e => e.Province)
                .WithMany(p => p.Communes)
                .HasForeignKey(e => e.ProvinceCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(BusinessRuleConstants.Models.Province.CodeMaxLength)
                .ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(BusinessRuleConstants.Models.Province.NameMaxLength);
        });

        builder.Entity<CandidateProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithOne(u => u.CandidateProfile)
                .HasForeignKey<CandidateProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.AvatarFilePath)
                .HasMaxLength(BusinessRuleConstants.Models.Candidate.AvatarFilePathMaxLength);
            entity.Property(e => e.Bio).HasMaxLength(BusinessRuleConstants.Models.Candidate.BioMaxLength);
            entity.Property(e => e.FullName).HasMaxLength(BusinessRuleConstants.Models.Candidate.FullNameMaxLength);

            entity.HasOne(e => e.PreferredWorkCommune)
                .WithMany()
                .HasForeignKey(e => e.PreferredWorkCommuneCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CandidateSchedule>(entity =>
        {
            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.CandidateSchedules)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployerRegistrationRequest>(entity =>
        {
            entity.Property(e => e.CompanyName)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.CompanyNameMaxLength);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.TaxCodeMaxLength);
            entity.Property(e => e.VerificationDocumentPath)
                .HasMaxLength(
                    BusinessRuleConstants.Models.EmployerRegistrationRequest.VerificationDocumentPathMaxLength);
            entity.Property(e => e.RejectReason)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.RejectReasonMaxLength);

            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.EmployerRegistrationRequests)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployerProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithOne(u => u.EmployerProfile)
                .HasForeignKey<EmployerProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.LogoFilePath)
                .HasMaxLength(BusinessRuleConstants.Models.Employer.LogoFilePathMaxLength);
            entity.Property(e => e.Description)
                .HasMaxLength(BusinessRuleConstants.Models.Employer.DescriptionMaxLength);
            entity.Property(e => e.ContactPhone).HasMaxLength(BusinessRuleConstants.Models.PhoneNumberLength)
                .IsFixedLength();
            entity.Property(e => e.CompanyName)
                .HasMaxLength(BusinessRuleConstants.Models.Employer.CompanyNameMaxLength);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(BusinessRuleConstants.Models.Employer.TaxCodeMaxLength);
            entity.Property(e => e.VerificationDocumentFilePath)
                .HasMaxLength(BusinessRuleConstants.Models.Employer.VerificationDocumentFilePathMaxLength);
        });

        builder.Entity<Industry>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(BusinessRuleConstants.Models.Industry.NameMaxLength);
        });

        builder.Entity<Job>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(BusinessRuleConstants.Models.Job.TitleMaxLength);
            entity.Property(e => e.Description).HasMaxLength(BusinessRuleConstants.Models.Job.DescriptionMaxLength);
            entity.Property(e => e.DetailAddress).HasMaxLength(BusinessRuleConstants.Models.Job.DetailAddressMaxLength);

            entity.HasOne(e => e.Commune)
                .WithMany()
                .HasForeignKey(e => e.CommuneCode)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employer)
                .WithMany(em => em.Jobs)
                .HasForeignKey(e => e.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Industry)
                .WithMany(i => i.Jobs)
                .HasForeignKey(e => e.IndustryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobSchedule>(entity =>
        {
            entity.HasOne(e => e.Job)
                .WithMany(j => j.JobSchedules)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobSalary>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.JobSalaries)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobApplication>(entity =>
        {
            entity.HasOne(e => e.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobReport>(entity =>
        {
            entity.Property(e => e.Reason).HasMaxLength(BusinessRuleConstants.Models.JobReport.ReasonMaxLength);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.JobReports)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reporter)
                .WithMany(c => c.JobReports)
                .HasForeignKey(e => e.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(BusinessRuleConstants.Models.Notification.TitleMaxLength);
            entity.Property(e => e.Content).HasMaxLength(BusinessRuleConstants.Models.Notification.ContentMaxLength);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum || Nullable.GetUnderlyingType(property.ClrType)?.IsEnum == true)
                {
                    property.SetProviderClrType(typeof(string));
                }
            }
        }
    }
}