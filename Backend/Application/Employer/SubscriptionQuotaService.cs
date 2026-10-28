using Domain;
using Domain.Models.Users;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Employer;

public class SubscriptionQuotaService(ApplicationDbContext context)
{
    public async Task<int> ApplyAsync(EmployerProfile employer, int extraActivePosts, DateTime now,
        CancellationToken cancellationToken = default)
    {
        employer.MaxActivePosts = BusinessRuleConstants.Employers.DefaultMaxActivePosts + extraActivePosts;

        var activeCount = await context.Jobs.CountAsync(
            job => job.EmployerId == employer.UserId && job.Status == JobStatus.Active,
            cancellationToken);
        var excessCount = activeCount - employer.MaxActivePosts;
        if (excessCount <= 0)
        {
            return 0;
        }

        var jobsToClose = await context.Jobs
            .Where(job => job.EmployerId == employer.UserId && job.Status == JobStatus.Active)
            .OrderBy(job => job.UpdatedAt)
            .ThenBy(job => job.Id)
            .Take(excessCount)
            .ToListAsync(cancellationToken);

        foreach (var job in jobsToClose)
        {
            job.Status = JobStatus.Closed;
            job.UpdatedAt = now;
        }

        return jobsToClose.Count;
    }
}
