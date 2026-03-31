using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEscrowRepository _escrowRepo;

        public WalletService(IWalletRepository walletRepo, IAuditLogRepository auditRepo, IUserRepository userRepo, IEscrowRepository escrowRepo)
        {
            _walletRepo = walletRepo;
            _auditRepo = auditRepo;
            _userRepo = userRepo;
            _escrowRepo = escrowRepo;
        }

        public async Task<WalletResponse> GetOrCreateWalletAsync(Guid userId)
        {
            var wallet = await _walletRepo.GetByUserIdAsync(userId);

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0m };
                await _walletRepo.AddAsync(wallet);
                await _walletRepo.SaveChangesAsync();
            }

            return new WalletResponse
            {
                UserId = wallet.UserId,
                OwnerName = (await _userRepo.GetByIdAsync(userId))?.Name ?? "Unknown",
                Balance = wallet.Balance,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// For the pilot/test phase: manually add test funds to a wallet.
        /// In production, this would be replaced by a Paystack/Flutterwave webhook.
        /// </summary>
        public async Task<WalletResponse> TopUpAsync(Guid userId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Top-up amount must be positive.");

            var wallet = await _walletRepo.GetByUserIdAsync(userId);

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = amount };
                await _walletRepo.AddAsync(wallet);
            }
            else
            {
                wallet.Balance += amount;
                _walletRepo.Update(wallet);
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Wallet",
                ReferenceId = wallet.Id,
                Action = "TOP_UP",
                ActorId = userId,
                Details = $"Credited ₦{amount:N2} to wallet. New balance: ₦{wallet.Balance:N2}."
            });

            await _walletRepo.SaveChangesAsync();
            await _auditRepo.SaveChangesAsync();

            return new WalletResponse
            {
                UserId = wallet.UserId,
                OwnerName = (await _userRepo.GetByIdAsync(userId))?.Name ?? "Unknown",
                Balance = wallet.Balance,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<WalletResponse> WithdrawAsync(Guid userId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be positive.");

            var wallet = await _walletRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Wallet not found. Please contact support.");

            if (wallet.Balance < amount)
                throw new InvalidOperationException(
                    $"Insufficient funds. Available: ₦{wallet.Balance:N2}, Requested: ₦{amount:N2}.");

            wallet.Balance -= amount;
            _walletRepo.Update(wallet);

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Wallet",
                ReferenceId = wallet.Id,
                Action = "WITHDRAW",
                ActorId = userId,
                Details = $"₦{amount:N2} withdrawn. Remaining balance: ₦{wallet.Balance:N2}."
            });

            await _walletRepo.SaveChangesAsync();
            await _auditRepo.SaveChangesAsync();

            return new WalletResponse
            {
                UserId = wallet.UserId,
                OwnerName = (await _userRepo.GetByIdAsync(userId))?.Name ?? "Unknown",
                Balance = wallet.Balance,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<List<AuditLogResponse>> GetTransactionsAsync(Guid userId)
        {
            // 1. All logs where this user was the actor (top-ups, escrow locks they initiated)
            var myLogs = await _auditRepo.GetForUserAsync(userId);

            // 2. Incoming payouts: FULL_RELEASE or REMOTE_RELEASE on escrows where this user is Provider
            var myEscrows = await _escrowRepo.GetByProviderIdAsync(userId);
            var escrowIds = myEscrows.Select(e => e.Id).ToHashSet();

            // We need all audit logs for those escrow IDs that are release actions
            // but NOT already captured by actor query (avoid duplicates)
            var alreadyHave = myLogs.Select(l => l.Id).ToHashSet();

            // Query DB directly via the repository's dbSet — using GetRecentLogsAsync is too broad,
            // so we piggyback off GetRecentLogsAsync and filter in memory (up to 200)
            var recentAll = (await _auditRepo.GetRecentLogsAsync(200)).ToList();
            var incomingPayouts = recentAll
                .Where(l => escrowIds.Contains(l.ReferenceId)
                         && (l.Action == "FULL_RELEASE" || l.Action == "REMOTE_RELEASE")
                         && !alreadyHave.Contains(l.Id))
                .ToList();

            var combined = myLogs.Concat(incomingPayouts)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new AuditLogResponse
                {
                    Id = l.Id,
                    ReferenceType = l.ReferenceType,
                    ReferenceId = l.ReferenceId,
                    Action = l.Action,
                    ActorId = l.ActorId,
                    Details = l.Details,
                    Timestamp = l.Timestamp
                })
                .ToList();

            return combined;
        }
    }
}
