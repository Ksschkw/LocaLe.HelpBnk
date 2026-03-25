using LocaLe.EscrowApi.DTOs;

namespace LocaLe.EscrowApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }

    public interface IJobService
    {
        Task<JobResponse> CreateJobAsync(int creatorId, CreateJobRequest request);
        Task<List<JobResponse>> GetOpenJobsAsync();
        Task<JobResponse?> GetJobByIdAsync(int jobId);
    }

    public interface IBookingService
    {
        Task<BookingResponse> ApplyToJobAsync(int jobId, int providerId);
        Task<BookingResponse> ConfirmBookingAsync(int bookingId, int buyerId);
        Task<List<BookingResponse>> GetBookingsForUserAsync(int userId);
    }

    public interface IEscrowService
    {
        /// <summary>
        /// Creates the escrow hold when a booking is confirmed.
        /// Deducts funds from the buyer's wallet and generates a QrToken.
        /// </summary>
        Task<EscrowResponse> SecureFundsAsync(int bookingId, int buyerId);

        /// <summary>
        /// Releases funds to the provider's wallet after QR verification.
        /// </summary>
        Task<EscrowResponse> ReleaseFundsAsync(int escrowId, string qrToken, int providerId);

        /// <summary>
        /// Freezes funds — neither buyer nor provider can access them until resolved.
        /// </summary>
        Task<EscrowResponse> DisputeAsync(int escrowId, int actorId);

        /// <summary>
        /// Cancels the escrow and refunds the buyer.
        /// </summary>
        Task<EscrowResponse> CancelAsync(int escrowId, int actorId);

        Task<EscrowResponse?> GetEscrowByBookingIdAsync(int bookingId);
        Task<List<AuditLogResponse>> GetAuditLogsAsync(int escrowId);
    }

    public interface IWalletService
    {
        Task<WalletResponse> GetOrCreateWalletAsync(int userId);
        Task<WalletResponse> TopUpAsync(int userId, decimal amount);
    }
}
