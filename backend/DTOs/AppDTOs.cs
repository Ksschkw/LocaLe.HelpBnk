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
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
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

    public class JobResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
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
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
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
        public int UserId { get; set; }
        public decimal Balance { get; set; }
    }

    // ─── Audit DTOs ──────────────────────────────────────────
    public class AuditLogResponse
    {
        public int Id { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int ActorId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
