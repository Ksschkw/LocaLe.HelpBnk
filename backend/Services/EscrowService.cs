using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class EscrowService : IEscrowService
    {
        private readonly EscrowContext _context;

        public EscrowService(EscrowContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Locks funds from the buyer's wallet into escrow.
        /// Called automatically when a buyer confirms a booking.
        /// </summary>
        public async Task<EscrowResponse> SecureFundsAsync(int bookingId, int buyerId)
        {
            // Use a transaction to ensure atomicity: either ALL of this succeeds or NONE of it does.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Job)
                    .FirstOrDefaultAsync(b => b.Id == bookingId)
                    ?? throw new InvalidOperationException("Booking not found.");

                if (booking.Job == null)
                    throw new InvalidOperationException("Associated job not found.");

                // Verify the buyer is the job creator
                if (booking.Job.CreatorId != buyerId)
                    throw new UnauthorizedAccessException("Only the job poster can secure funds.");

                // Check if escrow already exists for this booking
                var existingEscrow = await _context.Escrows.FirstOrDefaultAsync(e => e.BookingId == bookingId);
                if (existingEscrow != null)
                    throw new InvalidOperationException("Escrow already exists for this booking.");

                // Get buyer wallet and verify funds
                var buyerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == buyerId)
                    ?? throw new InvalidOperationException("Buyer wallet not found. Please top up first.");

                if (buyerWallet.Balance < booking.Job.Amount)
                    throw new InvalidOperationException(
                        $"Insufficient funds. Required: ₦{booking.Job.Amount:N2}, Available: ₦{buyerWallet.Balance:N2}.");

                // Deduct from buyer (the concurrency token on Wallet prevents double-spend)
                buyerWallet.Balance -= booking.Job.Amount;

                // Generate one-time QR verification token
                var qrToken = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

                var escrow = new Escrow
                {
                    BookingId = bookingId,
                    BuyerId = buyerId,
                    ProviderId = booking.ProviderId,
                    Amount = booking.Job.Amount,
                    Status = EscrowStatus.Secured,
                    QrToken = qrToken
                };

                _context.Escrows.Add(escrow);
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "SECURED",
                    ActorId = buyerId,
                    Details = $"₦{escrow.Amount:N2} locked from buyer #{buyerId}. QR token issued. Booking #{bookingId}."
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Releases escrowed funds to the provider after QR token verification.
        /// This is the "Job Done" moment — the provider gets paid instantly.
        /// </summary>
        public async Task<EscrowResponse> ReleaseFundsAsync(int escrowId, string qrToken, int providerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var escrow = await _context.Escrows.FindAsync(escrowId)
                    ?? throw new InvalidOperationException("Escrow not found.");

                // ─── State Machine Guard ─────────────────────────
                if (escrow.Status != EscrowStatus.Secured)
                    throw new InvalidOperationException(
                        $"Cannot release. Current escrow status: {escrow.Status}. Only 'Secured' escrows can be released.");

                // ─── QR Token Verification ───────────────────────
                if (!string.Equals(escrow.QrToken, qrToken, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Invalid QR token. Release denied.");

                // ─── Identity Verification ───────────────────────
                if (escrow.ProviderId != providerId)
                    throw new UnauthorizedAccessException("Only the assigned provider can release this escrow.");

                // ─── Transfer Funds ──────────────────────────────
                var providerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == escrow.ProviderId)
                    ?? throw new InvalidOperationException("Provider wallet not found.");

                providerWallet.Balance += escrow.Amount;
                escrow.Status = EscrowStatus.Released;
                escrow.QrToken = null; // Invalidate the token — one-time use only

                _context.AuditLogs.Add(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "RELEASED",
                    ActorId = providerId,
                    Details = $"QR verified. ₦{escrow.Amount:N2} released to provider #{providerId}."
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Freezes the escrow. Funds are locked and neither party can access them.
        /// Either the buyer or provider can trigger a dispute.
        /// </summary>
        public async Task<EscrowResponse> DisputeAsync(int escrowId, int actorId)
        {
            var escrow = await _context.Escrows.FindAsync(escrowId)
                ?? throw new InvalidOperationException("Escrow not found.");

            if (escrow.Status != EscrowStatus.Secured)
                throw new InvalidOperationException(
                    $"Cannot dispute. Current status: {escrow.Status}. Only 'Secured' escrows can be disputed.");

            // Either buyer or provider can file a dispute
            if (actorId != escrow.BuyerId && actorId != escrow.ProviderId)
                throw new UnauthorizedAccessException("Only the buyer or provider involved can dispute this escrow.");

            escrow.Status = EscrowStatus.InDispute;

            _context.AuditLogs.Add(new AuditLog
            {
                ReferenceType = "Escrow",
                ReferenceId = escrow.Id,
                Action = "DISPUTED",
                ActorId = actorId,
                Details = $"Dispute raised by user #{actorId}. ₦{escrow.Amount:N2} frozen pending resolution."
            });

            await _context.SaveChangesAsync();
            return ToResponse(escrow);
        }

        /// <summary>
        /// Cancels the escrow and refunds the buyer.
        /// Only possible when escrow is still in 'Secured' state.
        /// </summary>
        public async Task<EscrowResponse> CancelAsync(int escrowId, int actorId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var escrow = await _context.Escrows.FindAsync(escrowId)
                    ?? throw new InvalidOperationException("Escrow not found.");

                if (escrow.Status != EscrowStatus.Secured)
                    throw new InvalidOperationException(
                        $"Cannot cancel. Current status: {escrow.Status}. Only 'Secured' escrows can be cancelled.");

                // Only the buyer can cancel
                if (actorId != escrow.BuyerId)
                    throw new UnauthorizedAccessException("Only the buyer can cancel this escrow.");

                // Refund buyer
                var buyerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == escrow.BuyerId)
                    ?? throw new InvalidOperationException("Buyer wallet not found.");

                buyerWallet.Balance += escrow.Amount;
                escrow.Status = EscrowStatus.Cancelled;
                escrow.QrToken = null;

                _context.AuditLogs.Add(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "CANCELLED",
                    ActorId = actorId,
                    Details = $"Escrow cancelled. ₦{escrow.Amount:N2} refunded to buyer #{escrow.BuyerId}."
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<EscrowResponse?> GetEscrowByBookingIdAsync(int bookingId)
        {
            var escrow = await _context.Escrows.FirstOrDefaultAsync(e => e.BookingId == bookingId);
            return escrow == null ? null : ToResponse(escrow);
        }

        public async Task<List<AuditLogResponse>> GetAuditLogsAsync(int escrowId)
        {
            return await _context.AuditLogs
                .Where(a => a.ReferenceType == "Escrow" && a.ReferenceId == escrowId)
                .OrderBy(a => a.Timestamp)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    ReferenceType = a.ReferenceType,
                    ReferenceId = a.ReferenceId,
                    Action = a.Action,
                    ActorId = a.ActorId,
                    Details = a.Details,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();
        }

        private static EscrowResponse ToResponse(Escrow escrow) => new()
        {
            Id = escrow.Id,
            BookingId = escrow.BookingId,
            Amount = escrow.Amount,
            Status = escrow.Status.ToString(),
            QrToken = escrow.QrToken,
            CreatedAt = escrow.CreatedAt
        };
    }
}
