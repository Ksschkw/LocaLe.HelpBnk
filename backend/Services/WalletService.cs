using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class WalletService : IWalletService
    {
        private readonly EscrowContext _context;

        public WalletService(EscrowContext context)
        {
            _context = context;
        }

        public async Task<WalletResponse> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0m };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
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

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = amount };
                _context.Wallets.Add(wallet);
            }
            else
            {
                wallet.Balance += amount;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                ReferenceType = "Wallet",
                ReferenceId = wallet.Id,
                Action = "TOP_UP",
                ActorId = userId,
                Details = $"Credited ₦{amount:N2} to wallet. New balance: ₦{wallet.Balance:N2}."
            });

            await _context.SaveChangesAsync();

            return new WalletResponse { UserId = wallet.UserId, Balance = wallet.Balance };
        }
    }
}
