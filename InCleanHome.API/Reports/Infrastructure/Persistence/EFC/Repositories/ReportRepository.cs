using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Reports.Infrastructure.Persistence.EFC.Repositories;

public class ReportRepository(AppDbContext context)
    : BaseRepository<Report>(context), IReportRepository
{
    public async Task<IEnumerable<Report>> FindAllOrderedAsync()
        => await Context.Set<Report>()
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Report>> FindByReportedUserIdAsync(int reportedUserId)
        => await Context.Set<Report>()
            .Where(r => r.ReportedUserId == reportedUserId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Report>> FindConfirmedByReportedUserIdAsync(int reportedUserId)
        => await Context.Set<Report>()
            .Where(r => r.ReportedUserId == reportedUserId && r.Status == "confirmed")
            .OrderByDescending(r => r.ConfirmedAt ?? r.CreatedDate)
            .ToListAsync();

    public async Task<int> CountConfirmedByReportedUserIdAsync(int reportedUserId)
        => await Context.Set<Report>()
            .CountAsync(r => r.ReportedUserId == reportedUserId && r.Status == "confirmed");
}

