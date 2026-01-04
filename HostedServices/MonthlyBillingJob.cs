using FunderPayments.Services;

namespace FunderPayments.HostedServices;

public class MonthlyBillingJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyBillingJob> _logger;

    public MonthlyBillingJob(IServiceProvider serviceProvider, ILogger<MonthlyBillingJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
                await billingService.RunMonthlyBillingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly billing job failed");
            }

            // Run once per day; adjust as needed for production schedule.
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
        }
    }
}
