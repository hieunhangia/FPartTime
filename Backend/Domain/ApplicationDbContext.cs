using Domain.Models.Address;
using Domain.Models.EmployerSubscriptions;
using Domain.Models.Jobs;
using Domain.Models.Notifications;
using Domain.Models.Orders;
using Domain.Models.Users;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Domain;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Commune> Communes { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<CandidateProfile> Candidates { get; set; }
    public DbSet<EmployerRegistrationRequest> EmployerRegistrationRequests { get; set; }
    public DbSet<CandidateSchedule> CandidateSchedules { get; set; }
    public DbSet<EmployerProfile> Employers { get; set; }
    public DbSet<EmployerSubscription> EmployerSubscriptions { get; set; }
    public DbSet<EmployerSubscriptionPlan> EmployerSubscriptionPlans { get; set; }
    public DbSet<CensorProfile> Censors { get; set; }
    public DbSet<Industry> Industries { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<JobSchedule> JobSchedules { get; set; }
    public DbSet<JobSalary> JobSalaries { get; set; }
    public DbSet<JobReport> JobReports { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<OrderBase> Orders { get; set; }
    public DbSet<EmployerSubscriptionOrder> EmployerSubscriptionOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

            entity.HasIndex(e => e.OpenForJobUntil);
        });

        builder.Entity<CandidateSchedule>(entity =>
        {
            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.PreferredWorkSchedules)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployerRegistrationRequest>(entity =>
        {
            entity.Property(e => e.CompanyName)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.CompanyNameMaxLength);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.TaxCodeMaxLength);
            entity.Property(e => e.DocumentPath)
                .HasMaxLength(
                    BusinessRuleConstants.Models.EmployerRegistrationRequest.VerificationDocumentPathMaxLength);
            entity.Property(e => e.FullName)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.FullNameMaxLength);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(BusinessRuleConstants.Models.PhoneNumberLength)
                .IsFixedLength();
            entity.Property(e => e.CCCDNumber)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.CCCDNumberMaxLength);
            entity.Property(e => e.CCCDFrontImagePath)
                .HasMaxLength(
                    BusinessRuleConstants.Models.EmployerRegistrationRequest.VerificationDocumentPathMaxLength);
            entity.Property(e => e.CCCDBackImagePath)
                .HasMaxLength(
                    BusinessRuleConstants.Models.EmployerRegistrationRequest.VerificationDocumentPathMaxLength);
            entity.Property(e => e.RejectReason)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerRegistrationRequest.RejectReasonMaxLength);

            entity.HasOne(e => e.Requester)
                .WithMany(c => c.EmployerRegistrationRequests)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Processor)
                .WithMany(c => c.EmployerRegistrationRequests)
                .HasForeignKey(e => e.ProcessorId)
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

        builder.Entity<CensorProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithOne(u => u.CensorProfile)
                .HasForeignKey<CensorProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.HasOne(e => e.Resolver)
                .WithMany(c => c.JobReports)
                .HasForeignKey(e => e.ResolverId)
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

        builder.Entity<EmployerSubscription>(entity =>
        {
            entity.HasOne(e => e.Employer)
                .WithMany(em => em.Subscriptions)
                .HasForeignKey(e => e.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployerSubscriptionPlan>(entity =>
        {
            entity.HasKey(e => new { e.Tier, e.Duration });
            entity.Property(e => e.Tier)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.TierMaxLength)
                .ValueGeneratedNever();
            entity.Property(e => e.Duration)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.DurationMaxLength)
                .ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.NameMaxLength);
            entity.Property(e => e.Description)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.DescriptionMaxLength);
            entity.Property(e => e.Price)
                .HasPrecision(18, 2);
        });

        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(e => e.PaymentReferenceId)
                .HasMaxLength(BusinessRuleConstants.Models.PaymentTransaction.PaymentReferenceIdMaxLength);
            entity.HasIndex(e => e.PaymentReferenceId).IsUnique();
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.User)
                .WithMany(u => u.PaymentTransactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderBase>(entity =>
        {
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Description)
                .HasMaxLength(BusinessRuleConstants.Models.OrderBase.DescriptionMaxLength);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployerSubscriptionOrder>(entity =>
        {
            entity.Property(e => e.PlanName)
                .HasMaxLength(BusinessRuleConstants.Models.EmployerSubscriptionPlan.NameMaxLength);

            entity.HasOne(e => e.Employer)
                .WithMany(em => em.SubscriptionOrders)
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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<NullableUtcDateTimeConverter>();
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        d => d.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
            : d.ToUniversalTime(),
        d => DateTime.SpecifyKind(d, DateTimeKind.Utc))
    {
    }
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter() : base(
        d => !d.HasValue
            ? d
            : (d.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc)
                : d.Value.ToUniversalTime()),
        d => !d.HasValue ? d : DateTime.SpecifyKind(d.Value, DateTimeKind.Utc))
    {
    }
}