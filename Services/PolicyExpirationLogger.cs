using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.BackgroundServices;

public class PolicyExpirationLogger(
    ILogger<PolicyExpirationLogger> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30)); //twice an hour check => under 1 hour detection

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckForExpiredPoliciesAsync();
        }
    }

    private async Task CheckForExpiredPoliciesAsync()
    {
        logger.LogInformation("Background Service: Checking for recently expired policies...");


        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow); //UtcNow could be an issue in the future, but should not be the subject for this iteration

        var policiesToNotify = await dbContext.Policies
            .Where(p => p.EndDate < today &&           
                        !p.IsExpirationNotified)
            .ToListAsync();


        if (!policiesToNotify.Any())
        {
            logger.LogInformation("Background Service: No new policies have expired.");
            return;
        }

        foreach (var policy in policiesToNotify)
        {
            logger.LogWarning($"POLICY EXPIRED: Policy ID {policy.Id} for Car ID {policy.CarId} expired at {policy.EndDate:yyyy-MM-dd}.");
            policy.IsExpirationNotified = true;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation($"Background Service: Processed and logged {policiesToNotify.Count} expired policies.");
    }
}