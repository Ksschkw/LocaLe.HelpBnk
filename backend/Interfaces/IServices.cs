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
        Task<JobResponse> CreateJobAsync(Guid creatorId, CreateJobRequest request);
        Task<JobResponse> UpdateJobAsync(Guid creatorId, Guid jobId, UpdateJobRequest request);
        Task DeleteJobAsync(Guid creatorId, Guid jobId);
        Task<List<JobResponse>> GetOpenJobsAsync();
        Task<JobResponse?> GetJobByIdAsync(Guid jobId);
        Task<List<JobResponse>> GetMyRequestsAsync(Guid creatorId);
        Task<List<JobResponse>> GetMyServiceRequestsAsync(Guid providerId);
        Task<JobResponse> ConfirmCompletionAsync(Guid creatorId, Guid jobId);
        Task<JobResponse> CreateJobForServiceAsync(Guid creatorId, Guid serviceId, CreateJobRequest request);
    }

    public interface IBookingService
    {
        Task<BookingResponse> ApplyToJobAsync(Guid jobId, Guid providerId);
        Task<BookingResponse> AcceptJobAsync(Guid jobId, Guid providerId);
        Task<BookingResponse> ConfirmBookingAsync(Guid bookingId, Guid buyerId, decimal initialDepositPercent = 1.0m);
        Task<BookingResponse> UpdateBookingStatusAsync(Guid bookingId, Guid userId, string newStatus);
        Task DeleteBookingAsync(Guid bookingId, Guid userId);
        Task<List<BookingResponse>> GetBookingsForUserAsync(Guid userId);
    }

    public interface IEscrowService
    {
        /// <summary>
        /// Creates the escrow hold when a booking is confirmed.
        /// Deducts a percentage of funds (e.g. 50%) from the buyer's wallet.
        /// </summary>
        Task<EscrowResponse> SecureFundsAsync(Guid bookingId, Guid buyerId, decimal initialDepositPercent = 1.0m);

        /// <summary>
        /// Secures the remaining funds from the buyer (if partial) and releases 100% to the provider.
        /// </summary>
        Task<EscrowResponse> CompleteAndReleaseAsync(Guid escrowId, string qrToken, Guid providerId);

        /// <summary>
        /// Releases funds to the provider's wallet after QR verification. (Kept for backward compatibility)
        /// </summary>
        Task<EscrowResponse> ReleaseFundsAsync(Guid escrowId, string qrToken, Guid providerId);

        /// <summary>
        /// Freezes funds — neither buyer nor provider can access them until resolved.
        /// </summary>
        Task<EscrowResponse> DisputeAsync(Guid escrowId, Guid actorId);

        /// <summary>
        /// Cancels the escrow and refunds the buyer.
        /// </summary>
        Task<EscrowResponse> CancelAsync(Guid escrowId, Guid actorId);

        Task<EscrowResponse?> GetEscrowByBookingIdAsync(Guid bookingId);
        Task<List<AuditLogResponse>> GetAuditLogsAsync(Guid escrowId);

        /// <summary>
        /// Generates a new QR token for a secured escrow if the old one expired.
        /// </summary>
        Task<EscrowResponse> RefreshQrAsync(Guid escrowId, Guid buyerId);
    }

    public interface IWalletService
    {
        Task<WalletResponse> GetOrCreateWalletAsync(Guid userId);
        Task<WalletResponse> TopUpAsync(Guid userId, decimal amount);
        Task<WalletResponse> WithdrawAsync(Guid userId, decimal amount);
        Task<List<AuditLogResponse>> GetTransactionsAsync(Guid userId);
    }
}
