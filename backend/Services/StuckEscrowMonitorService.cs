using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class StuckEscrowMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StuckEscrowMonitorService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _stuckThreshold = TimeSpan.FromHours(24); // Configurable

        public StuckEscrowMonitorService(IServiceProvider serviceProvider, ILogger<StuckEscrowMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stuck Escrow Monitor Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckForStuckEscrowsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckForStuckEscrowsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EscrowContext>();

                var stuckThresholdTime = DateTime.UtcNow.Subtract(_stuckThreshold);

                var stuckEscrows = await dbContext.Escrows
                    .Where(e => e.Status == EscrowStatus.Secured && e.CreatedAt <= stuckThresholdTime)
                    .ToListAsync(cancellationToken);

                foreach (var escrow in stuckEscrows)
                {
                    // Check if we already logged a stuck alert recently to avoid spam
                    var hasRecentAlert = await dbContext.AuditLogs
                        .AnyAsync(a => a.ReferenceId == escrow.Id && a.Action == "STUCK_ESCROW_ALERT" 
                                    && a.Timestamp >= DateTime.UtcNow.Subtract(TimeSpan.FromHours(12)), 
                                  cancellationToken);

                    if (!hasRecentAlert)
                    {
                        var alertLog = new AuditLog
                        {
                            JobId = escrow.BookingId, // Mapping via bookingid if Jobid is not directly here, or we can look it up
                            ReferenceType = "Escrow",
                            ReferenceId = escrow.Id,
                            Action = "STUCK_ESCROW_ALERT",
                            ActorId = Guid.Empty, // System
                            Details = $"Escrow {escrow.Id} has been in Secured state for over {_stuckThreshold.TotalHours} hours without release.",
                            Timestamp = DateTime.UtcNow
                        };

                        dbContext.AuditLogs.Add(alertLog);
                        _logger.LogWarning($"STUCK ESCROW DETECTED: {escrow.Id}");
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for stuck escrows.");
            }
        }
    }
}
