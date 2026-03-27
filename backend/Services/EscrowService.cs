using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class EscrowService : IEscrowService
    {
        private readonly IEscrowRepository _escrowRepo;
        private readonly IBookingRepository _bookingRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IAuditLogRepository _auditRepo;

        public EscrowService(
            IEscrowRepository escrowRepo,
            IBookingRepository bookingRepo,
            IWalletRepository walletRepo,
            IAuditLogRepository auditRepo)
        {
            _escrowRepo = escrowRepo;
            _bookingRepo = bookingRepo;
            _walletRepo = walletRepo;
            _auditRepo = auditRepo;
        }

        public async Task<EscrowResponse> SecureFundsAsync(int bookingId, int buyerId, decimal initialDepositPercent = 1.0m)
        {
            using var transaction = await _escrowRepo.BeginTransactionAsync();

            try
            {
                var booking = await _bookingRepo.GetBookingDetailedAsync(bookingId)
                    ?? throw new InvalidOperationException("Booking not found.");

                if (booking.Job == null)
                    throw new InvalidOperationException("Associated job not found.");

                if (booking.Job.CreatorId != buyerId)
                    throw new UnauthorizedAccessException("Only the job poster can secure funds.");

                var existingEscrow = await _escrowRepo.GetEscrowByBookingIdAsync(bookingId);
                if (existingEscrow != null)
                    throw new InvalidOperationException("Escrow already exists for this booking.");

                var buyerWallet = await _walletRepo.GetByUserIdAsync(buyerId)
                    ?? throw new InvalidOperationException("Buyer wallet not found. Please top up first.");

                var initialAmount = booking.Job.Amount * initialDepositPercent;
                if (buyerWallet.Balance < initialAmount)
                    throw new InvalidOperationException(
                        $"Insufficient funds for phase 1. Required: ₦{initialAmount:N2}, Available: ₦{buyerWallet.Balance:N2}.");

                buyerWallet.Balance -= initialAmount;
                _walletRepo.Update(buyerWallet);

                var qrToken = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

                var escrow = new Escrow
                {
                    BookingId = bookingId,
                    BuyerId = buyerId,
                    ProviderId = booking.ProviderId,
                    Amount = booking.Job.Amount,
                    InitialDepositPercentage = initialDepositPercent,
                    Status = EscrowStatus.Secured,
                    QrToken = qrToken
                };

                await _escrowRepo.AddAsync(escrow);
                await _escrowRepo.SaveChangesAsync();
                await _walletRepo.SaveChangesAsync();

                await _auditRepo.AddAsync(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "SECURED",
                    ActorId = buyerId,
                    Details = $"₦{initialAmount:N2} ({initialDepositPercent:P0}) locked from buyer #{buyerId}. Booking #{bookingId}."
                });

                await _auditRepo.SaveChangesAsync();
                await _escrowRepo.CommitTransactionAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await _escrowRepo.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<EscrowResponse> CompleteAndReleaseAsync(int escrowId, string qrToken, int providerId)
        {
            using var transaction = await _escrowRepo.BeginTransactionAsync();

            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId)
                    ?? throw new InvalidOperationException("Escrow not found.");

                if (escrow.Status != EscrowStatus.Secured)
                    throw new InvalidOperationException("Escrow not in secured state.");

                if (!string.Equals(escrow.QrToken, qrToken, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Invalid QR token.");

                if (escrow.ProviderId != providerId)
                    throw new UnauthorizedAccessException("Permission denied.");

                // Phase 2: Secure remaining funds if partial
                if (!escrow.IsSecondPhaseFunded && escrow.InitialDepositPercentage < 1.0m)
                {
                    var buyerWallet = await _walletRepo.GetByUserIdAsync(escrow.BuyerId)
                        ?? throw new InvalidOperationException("Buyer wallet not found.");

                    if (buyerWallet.Balance < escrow.SecondPhaseAmount)
                        throw new InvalidOperationException($"Buyer has insufficient funds for the remaining ₦{escrow.SecondPhaseAmount:N2}.");

                    buyerWallet.Balance -= escrow.SecondPhaseAmount;
                    escrow.IsSecondPhaseFunded = true;
                    _walletRepo.Update(buyerWallet);
                    await _walletRepo.SaveChangesAsync();

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        ReferenceType = "Escrow",
                        ReferenceId = escrow.Id,
                        Action = "REMAINING_FUNDS_SECURED",
                        ActorId = escrow.BuyerId,
                        Details = $"Final ₦{escrow.SecondPhaseAmount:N2} secured before release."
                    });
                }

                // Release total to provider
                var providerWallet = await _walletRepo.GetByUserIdAsync(escrow.ProviderId)
                    ?? throw new InvalidOperationException("Provider wallet not found.");

                providerWallet.Balance += escrow.Amount;
                _walletRepo.Update(providerWallet);

                escrow.Status = EscrowStatus.Released;
                escrow.QrToken = null;
                _escrowRepo.Update(escrow);

                await _auditRepo.AddAsync(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "FULL_RELEASE",
                    ActorId = providerId,
                    Details = $"Full payout of ₦{escrow.Amount:N2} completed to provider #{providerId}."
                });

                await _escrowRepo.SaveChangesAsync();
                await _auditRepo.SaveChangesAsync();
                await _escrowRepo.CommitTransactionAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await _escrowRepo.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<EscrowResponse> ReleaseFundsAsync(int escrowId, string qrToken, int providerId)
        {
            // Simple bridge to the new logic if it's 100% or just handle as 100%
            return await CompleteAndReleaseAsync(escrowId, qrToken, providerId);
        }

        public async Task<EscrowResponse> DisputeAsync(int escrowId, int actorId)
        {
            var escrow = await _escrowRepo.GetByIdAsync(escrowId)
                ?? throw new InvalidOperationException("Escrow not found.");

            if (escrow.Status != EscrowStatus.Secured)
                throw new InvalidOperationException(
                    $"Cannot dispute. Current status: {escrow.Status}. Only 'Secured' escrows can be disputed.");

            if (actorId != escrow.BuyerId && actorId != escrow.ProviderId)
                throw new UnauthorizedAccessException("Only the buyer or provider involved can dispute this escrow.");

            escrow.Status = EscrowStatus.InDispute;
            _escrowRepo.Update(escrow);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Escrow",
                ReferenceId = escrow.Id,
                Action = "DISPUTED",
                ActorId = actorId,
                Details = $"Dispute raised by user #{actorId}. ₦{escrow.Amount:N2} frozen pending resolution."
            });

            await _escrowRepo.SaveChangesAsync();
            await _auditRepo.SaveChangesAsync();
            return ToResponse(escrow);
        }

        public async Task<EscrowResponse> CancelAsync(int escrowId, int actorId)
        {
            using var transaction = await _escrowRepo.BeginTransactionAsync();

            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(escrowId)
                    ?? throw new InvalidOperationException("Escrow not found.");

                if (escrow.Status != EscrowStatus.Secured)
                    throw new InvalidOperationException(
                        $"Cannot cancel. Current status: {escrow.Status}. Only 'Secured' escrows can be cancelled.");

                if (actorId != escrow.BuyerId)
                    throw new UnauthorizedAccessException("Only the buyer can cancel this escrow.");

                var buyerWallet = await _walletRepo.GetByUserIdAsync(escrow.BuyerId)
                    ?? throw new InvalidOperationException("Buyer wallet not found.");

                buyerWallet.Balance += escrow.Amount;
                _walletRepo.Update(buyerWallet);

                escrow.Status = EscrowStatus.Cancelled;
                escrow.QrToken = null;
                _escrowRepo.Update(escrow);

                await _auditRepo.AddAsync(new AuditLog
                {
                    ReferenceType = "Escrow",
                    ReferenceId = escrow.Id,
                    Action = "CANCELLED",
                    ActorId = actorId,
                    Details = $"Escrow cancelled. ₦{escrow.Amount:N2} refunded to buyer #{escrow.BuyerId}."
                });

                await _escrowRepo.SaveChangesAsync();
                await _walletRepo.SaveChangesAsync();
                await _auditRepo.SaveChangesAsync();
                await _escrowRepo.CommitTransactionAsync();

                return ToResponse(escrow);
            }
            catch
            {
                await _escrowRepo.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<EscrowResponse?> GetEscrowByBookingIdAsync(int bookingId)
        {
            var escrow = await _escrowRepo.GetEscrowByBookingIdAsync(bookingId);
            return escrow == null ? null : ToResponse(escrow);
        }

        public async Task<List<AuditLogResponse>> GetAuditLogsAsync(int escrowId)
        {
            // Simple query via generic find if not specialized
            var logs = await _auditRepo.FindAsync(a => a.ReferenceType == "Escrow" && a.ReferenceId == escrowId);
            return logs.OrderBy(a => a.Timestamp)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    ReferenceType = a.ReferenceType,
                    ReferenceId = a.ReferenceId,
                    Action = a.Action,
                    ActorId = a.ActorId,
                    Details = a.Details,
                    Timestamp = a.Timestamp
                }).ToList();
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
