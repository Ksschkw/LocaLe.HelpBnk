using System.ComponentModel.DataAnnotations;

namespace LocaLe.EscrowApi.DTOs
{
    // ─── Auth DTOs ───────────────────────────────────────────
    public class RegisterRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// When true, the JWT cookie will have a longer expiry (30 days).
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Role string: "User", "Admin", or "SuperAdmin".</summary>
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    // ─── Job DTOs ────────────────────────────────────────────
    public class CreateJobRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }

    public class UpdateJobRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public decimal? Amount { get; set; }
        public string? Status { get; set; }
    }

    public class JobResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>Open | Assigned | Completed | Cancelled</summary>
        public string Status { get; set; } = string.Empty;
        public Guid CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        /// <summary>The Service this request was made against (if a direct service request).</summary>
        public Guid? ServiceId { get; set; }
        public string? ServiceTitle { get; set; }
        /// <summary>If Assigned — who accepted the job.</summary>
        public string? AssignedProviderName { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Human-friendly context: e.g. "Awaiting provider acceptance" or "Funds locked in escrow".</summary>
        public string StatusDetail { get; set; } = string.Empty;
    }

    // ─── Booking DTOs ────────────────────────────────────────
    public class ApplyToJobRequest
    {
        // No body needed; the provider is identified from the JWT,
        // and the job ID comes from the route.
    }

    public class ConfirmBookingRequest
    {
        // No body needed; the booking ID comes from the route,
        // and the buyer is identified from the JWT.
    }

    public class BookingResponse
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public decimal JobAmount { get; set; }
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        /// <summary>Pending | Active | Finalized | Cancelled</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>Whether escrow funds have been locked for this booking.</summary>
        public bool EscrowSecured { get; set; }
        public Guid? EscrowId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Escrow DTOs ─────────────────────────────────────────
    public class ReleaseEscrowRequest
    {
        [Required]
        public string QrToken { get; set; } = string.Empty;
    }

    public class EscrowResponse
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        /// <summary>Friendly: same as the Job ID. The Booking ties a Job to its chosen Provider.</summary>
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>Secured | Released | Cancelled | InDispute</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>QR code token the provider presents upon job completion to release funds.</summary>
        public string? QrToken { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Wallet DTOs ─────────────────────────────────────────
    public class TopUpRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class WalletResponse
    {
        public Guid UserId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string FormattedBalance => $"₦{Balance:N2}";
        public DateTime LastUpdated { get; set; }
    }

    // ─── Audit DTOs ──────────────────────────────────────────
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public Guid ReferenceId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid ActorId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // ─── Admin DTOs ──────────────────────────────────────────
    public class AdminUserResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TrustScore { get; set; }
        public int VerificationLevel { get; set; }
        public bool IsNinVerified { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string Tier { get; set; } = string.Empty;
        public int TotalVouchPoints { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetRoleRequest
    {
        /// <summary>Target role: "User", "Admin", or "SuperAdmin". Only a SuperAdmin can set roles.</summary>
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class CreateAdminRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Specific role: "Admin" or "SuperAdmin".</summary>
        [Required]
        public string Role { get; set; } = "Admin";
    }

    public class AdminJobResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminDisputeResponse
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public Guid RaisedById { get; set; }
        public string RaisedByName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ResolveDisputeRequest
    {
        [Required, MaxLength(1000)]
        public string Resolution { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // ─── Community DTOs ──────────────────────────────────────
    public class VouchResponse
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public Guid VoucherId { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public int PointsGiven { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WaitlistResponse
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceTitle { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AgreeWaitlistTermsRequest
    {
        public decimal InitialDepositPercent { get; set; }
    }
}
