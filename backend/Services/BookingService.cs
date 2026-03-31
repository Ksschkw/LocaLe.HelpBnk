using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IEscrowService _escrowService;

        public BookingService(
            IBookingRepository bookingRepo,
            IJobRepository jobRepo,
            IUserRepository userRepo,
            IAuditLogRepository auditRepo,
            IEscrowService escrowService)
        {
            _bookingRepo = bookingRepo;
            _jobRepo = jobRepo;
            _userRepo = userRepo;
            _auditRepo = auditRepo;
            _escrowService = escrowService;
        }

        public async Task<BookingResponse> ApplyToJobAsync(Guid jobId, Guid providerId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId)
                ?? throw new InvalidOperationException("Job not found.");

            if (job.Status != JobStatus.Open)
                throw new InvalidOperationException($"Cannot apply. Job status is {job.Status}.");

            if (job.CreatorId == providerId)
                throw new InvalidOperationException("You cannot apply to your own job.");

            var alreadyApplied = await _bookingRepo.HasUserAppliedAsync(jobId, providerId);
            if (alreadyApplied)
                throw new InvalidOperationException("You have already applied for this job.");

            var provider = await _userRepo.GetByIdAsync(providerId)
                ?? throw new InvalidOperationException("Provider not found.");

            var booking = new Booking
            {
                JobId = jobId,
                ProviderId = providerId,
                Status = BookingStatus.Pending
            };

            await _bookingRepo.AddAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Booking",
                ReferenceId = booking.Id,
                Action = "APPLIED",
                ActorId = providerId,
                Details = $"{provider.Name} applied for job {jobId} '{job.Title}'."
            });
            await _auditRepo.SaveChangesAsync();

            return new BookingResponse
            {
                Id = booking.Id,
                JobId = booking.JobId,
                JobTitle = job.Title,
                ProviderId = booking.ProviderId,
                ProviderName = provider.Name,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        /// <summary>
        /// A direct accept flow for service-request jobs.
        /// Provider accepts → Booking is created → Escrow is locked immediately.
        /// The Buyer's wallet is debited for the full job amount on the spot.
        /// </summary>
        public async Task<BookingResponse> AcceptJobAsync(Guid jobId, Guid providerId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId)
                ?? throw new KeyNotFoundException("Job not found.");

            if (job.Status != JobStatus.Open)
                throw new InvalidOperationException($"This job is no longer available. Status: {job.Status}.");

            if (job.CreatorId == providerId)
                throw new InvalidOperationException("You cannot accept your own job.");

            var provider = await _userRepo.GetByIdAsync(providerId)
                ?? throw new InvalidOperationException("Provider not found.");

            // Create the booking record linking Job and Provider
            var booking = new Booking
            {
                JobId = jobId,
                ProviderId = providerId,
                Status = BookingStatus.Active // Skip Pending — this is a direct accept
            };

            await _bookingRepo.AddAsync(booking);

            // Mark the job as Assigned so no one else can book it
            job.Status = JobStatus.Assigned;
            _jobRepo.Update(job);

            await _bookingRepo.SaveChangesAsync();
            await _jobRepo.SaveChangesAsync();

            // Lock the BUYER'S funds immediately into escrow
            await _escrowService.SecureFundsAsync(booking.Id, job.CreatorId, 1.0m);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Booking",
                ReferenceId = booking.Id,
                Action = "ACCEPTED",
                ActorId = providerId,
                Details = $"{provider.Name} accepted job '{job.Title}'. Escrow secured. Buyer: {job.CreatorId}."
            });
            await _auditRepo.SaveChangesAsync();

            return new BookingResponse
            {
                Id = booking.Id,
                JobId = booking.JobId,
                JobTitle = job.Title,
                ProviderId = booking.ProviderId,
                ProviderName = provider.Name,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        public async Task<BookingResponse> ConfirmBookingAsync(Guid bookingId, Guid buyerId, decimal initialDepositPercent = 1.0m)
        {
            var booking = await _bookingRepo.GetBookingDetailedAsync(bookingId)
                ?? throw new InvalidOperationException("Booking not found.");

            if (booking.Job == null)
                throw new InvalidOperationException("Associated job not found.");

            if (booking.Job.CreatorId != buyerId)
                throw new UnauthorizedAccessException("Only the job poster can confirm a booking.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm. Booking status is {booking.Status}.");

            booking.Status = BookingStatus.Active;
            booking.Job.Status = JobStatus.Assigned;

            _bookingRepo.Update(booking);
            _jobRepo.Update(booking.Job);
            await _bookingRepo.SaveChangesAsync();
            await _jobRepo.SaveChangesAsync();

            // Trigger escrow: lock the buyer's funds (partial or full)
            await _escrowService.SecureFundsAsync(booking.Id, buyerId, initialDepositPercent);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Booking",
                ReferenceId = booking.Id,
                Action = "CONFIRMED",
                ActorId = buyerId,
                Details = $"Booking {bookingId} confirmed. Provider: {booking.Provider?.Name}. Escrow secured."
            });
            await _auditRepo.SaveChangesAsync();

            return new BookingResponse
            {
                Id = booking.Id,
                JobId = booking.JobId,
                JobTitle = booking.Job.Title,
                ProviderId = booking.ProviderId,
                ProviderName = booking.Provider?.Name ?? "Unknown",
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        public async Task<BookingResponse> UpdateBookingStatusAsync(Guid bookingId, Guid userId, string newStatus)
        {
            var booking = await _bookingRepo.GetBookingDetailedAsync(bookingId)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.ProviderId != userId && (booking.Job == null || booking.Job.CreatorId != userId))
                throw new UnauthorizedAccessException("You don't have permission to update this booking.");

            if (Enum.TryParse<BookingStatus>(newStatus, true, out var parsedStatus))
                booking.Status = parsedStatus;
            else
                throw new InvalidOperationException("Invalid booking status.");

            _bookingRepo.Update(booking);
            await _bookingRepo.SaveChangesAsync();

            return new BookingResponse
            {
                Id = booking.Id,
                JobId = booking.JobId,
                JobTitle = booking.Job?.Title ?? "Unknown",
                ProviderId = booking.ProviderId,
                ProviderName = booking.Provider?.Name ?? "Unknown",
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        public async Task DeleteBookingAsync(Guid bookingId, Guid userId)
        {
            var booking = await _bookingRepo.GetBookingDetailedAsync(bookingId)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.ProviderId != userId && (booking.Job == null || booking.Job.CreatorId != userId))
                throw new UnauthorizedAccessException("You don't have permission to delete this booking.");

            if (booking.Status == BookingStatus.Active || booking.Status == BookingStatus.Finalized)
                throw new InvalidOperationException($"Cannot delete booking in {booking.Status} state.");

            _bookingRepo.Remove(booking);
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task<List<BookingResponse>> GetBookingsForUserAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetUserBookingsAsync(userId);

            return bookings.Select(b => new BookingResponse
            {
                Id = b.Id,
                JobId = b.JobId,
                JobTitle = b.Job != null ? b.Job.Title : "Unknown",
                ProviderId = b.ProviderId,
                ProviderName = b.Provider != null ? b.Provider.Name : "Unknown",
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt
            }).ToList();
        }
    }
}
