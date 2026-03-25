using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly EscrowContext _context;
        private readonly IEscrowService _escrowService;

        public BookingService(EscrowContext context, IEscrowService escrowService)
        {
            _context = context;
            _escrowService = escrowService;
        }

        /// <summary>
        /// A provider applies for an open job. This creates a Pending booking.
        /// </summary>
        public async Task<BookingResponse> ApplyToJobAsync(int jobId, int providerId)
        {
            var job = await _context.Jobs.FindAsync(jobId)
                ?? throw new InvalidOperationException("Job not found.");

            if (job.Status != JobStatus.Open)
                throw new InvalidOperationException($"Cannot apply. Job status is {job.Status}.");

            if (job.CreatorId == providerId)
                throw new InvalidOperationException("You cannot apply to your own job.");

            // Check if provider already applied
            var alreadyApplied = await _context.Bookings
                .AnyAsync(b => b.JobId == jobId && b.ProviderId == providerId && b.Status == BookingStatus.Pending);
            if (alreadyApplied)
                throw new InvalidOperationException("You have already applied for this job.");

            var provider = await _context.Users.FindAsync(providerId)
                ?? throw new InvalidOperationException("Provider not found.");

            var booking = new Booking
            {
                JobId = jobId,
                ProviderId = providerId,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);

            _context.AuditLogs.Add(new AuditLog
            {
                ReferenceType = "Booking",
                ReferenceId = 0,
                Action = "APPLIED",
                ActorId = providerId,
                Details = $"{provider.Name} applied for job #{jobId} '{job.Title}'."
            });

            await _context.SaveChangesAsync();

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
        /// The buyer confirms a booking, changing the job to Assigned
        /// and triggering the escrow fund lock.
        /// </summary>
        public async Task<BookingResponse> ConfirmBookingAsync(int bookingId, int buyerId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Job)
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new InvalidOperationException("Booking not found.");

            if (booking.Job == null)
                throw new InvalidOperationException("Associated job not found.");

            // Only the job creator (buyer) can confirm
            if (booking.Job.CreatorId != buyerId)
                throw new UnauthorizedAccessException("Only the job poster can confirm a booking.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm. Booking status is {booking.Status}.");

            // Update statuses
            booking.Status = BookingStatus.Active;
            booking.Job.Status = JobStatus.Assigned;

            await _context.SaveChangesAsync();

            // Trigger escrow: lock the buyer's funds
            await _escrowService.SecureFundsAsync(booking.Id, buyerId);

            _context.AuditLogs.Add(new AuditLog
            {
                ReferenceType = "Booking",
                ReferenceId = booking.Id,
                Action = "CONFIRMED",
                ActorId = buyerId,
                Details = $"Booking #{bookingId} confirmed. Provider: {booking.Provider?.Name}. Escrow secured."
            });

            await _context.SaveChangesAsync();

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

        public async Task<List<BookingResponse>> GetBookingsForUserAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Job)
                .Include(b => b.Provider)
                .Where(b => b.ProviderId == userId || (b.Job != null && b.Job.CreatorId == userId))
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingResponse
                {
                    Id = b.Id,
                    JobId = b.JobId,
                    JobTitle = b.Job != null ? b.Job.Title : "Unknown",
                    ProviderId = b.ProviderId,
                    ProviderName = b.Provider != null ? b.Provider.Name : "Unknown",
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }
    }
}
