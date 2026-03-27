using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public interface IWaitlistService
    {
        Task JoinWaitlistAsync(Guid serviceId, Guid userId, string? notes);
        Task<IEnumerable<Waitlist>> GetUserWaitlistAsync(Guid userId);
        Task AgreeToTermsAsync(Guid waitlistId, Guid userId, decimal initialDepositPercent);
    }

    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _waitlistRepo;
        private readonly IServiceRepository _serviceRepo;
        private readonly IJobService _jobService;
        private readonly IBookingService _bookingService;
        private readonly IAuditLogRepository _auditRepo;

        public WaitlistService(
            IWaitlistRepository waitlistRepo,
            IServiceRepository serviceRepo,
            IJobService jobService,
            IBookingService bookingService,
            IAuditLogRepository auditRepo)
        {
            _waitlistRepo = waitlistRepo;
            _serviceRepo = serviceRepo;
            _jobService = jobService;
            _bookingService = bookingService;
            _auditRepo = auditRepo;
        }

        public async Task JoinWaitlistAsync(int serviceId, int userId, string? notes)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId)
                ?? throw new KeyNotFoundException("Service not found.");

            if (!service.IsDiscoveryEnabled)
                throw new InvalidOperationException("This service is not yet enabled for waitlisting (referral threshold not met).");

            var waitlist = new Waitlist
            {
                ServiceId = serviceId,
                UserId = userId,
                Status = WaitlistStatus.Pending,
                PrivateNotes = notes
            };

            await _waitlistRepo.AddAsync(waitlist);
            await _waitlistRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Waitlist",
                ReferenceId = waitlist.Id,
                Action = "JoinedWaitlist",
                ActorId = userId,
                Details = $"User joined waitlist for service #{serviceId}."
            });
            await _auditRepo.SaveChangesAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetUserWaitlistAsync(int userId)
        {
            return await _waitlistRepo.GetByUserIdAsync(userId);
        }

        public async Task AgreeToTermsAsync(int waitlistId, int userId, decimal initialDepositPercent)
        {
            var waitlist = await _waitlistRepo.GetWithDetailsAsync(waitlistId)
                ?? throw new KeyNotFoundException("Waitlist entry not found.");

            if (waitlist.UserId != userId)
                throw new UnauthorizedAccessException("You can only agree to your own waitlist entries.");

            if (waitlist.Status != WaitlistStatus.Pending)
                throw new InvalidOperationException("Already processed.");

            waitlist.Status = WaitlistStatus.Agreed;
            _waitlistRepo.Update(waitlist);

            // Create a Job from this agreement
            var createJobReq = new CreateJobRequest
            {
                Title = $"Engagement: {waitlist.Service?.Title}",
                Description = $"Service engagement agreement from waitlist. {waitlist.PrivateNotes}",
                Amount = waitlist.Service?.BasePrice ?? 0
            };

            var job = await _jobService.CreateJobAsync(userId, createJobReq);

            // Automatically apply and confirm to bypass manual steps if agreed
            var booking = await _bookingService.ApplyToJobAsync(job.Id, waitlist.Service!.ProviderId);
            
            // Note: The Escrow phase 1 (partial deposit) will be handled in the EscrowService
            // which we'll update next. For now, we use the booking confirm which normally locks 100%.
            // We'll update the EscrowService logic to look for a special flag or percentage.
            
            await _bookingService.ConfirmBookingAsync(booking.Id, userId, initialDepositPercent);

            await _waitlistRepo.SaveChangesAsync();
        }
    }
}
