using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly IAuditLogRepository _auditRepo;

        public WalletService(IWalletRepository walletRepo, IAuditLogRepository auditRepo)
        {
            _walletRepo = walletRepo;
            _auditRepo = auditRepo;
        }

        public async Task<WalletResponse> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await _walletRepo.GetByUserIdAsync(userId);

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0m };
                await _walletRepo.AddAsync(wallet);
                await _walletRepo.SaveChangesAsync();
            }

            return new WalletResponse { UserId = wallet.UserId, Balance = wallet.Balance };
        }

        /// <summary>
        /// For the pilot/test phase: manually add test funds to a wallet.
        /// In production, this would be replaced by a Paystack/Flutterwave webhook.
        /// </summary>
        public async Task<WalletResponse> TopUpAsync(int userId, decimal amount)
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

            return new WalletResponse { UserId = wallet.UserId, Balance = wallet.Balance };
        }
    }
}
