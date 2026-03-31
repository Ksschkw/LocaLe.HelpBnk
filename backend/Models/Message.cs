using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocaLe.EscrowApi.Models
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid JobId { get; set; }

        [ForeignKey("JobId")]
        [JsonIgnore]
        public Job? Job { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [ForeignKey("SenderId")]
        [JsonIgnore]
        public User? Sender { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// If set, this message is a reply to another message (threading).
        /// </summary>
        public Guid? ParentMessageId { get; set; }

        [ForeignKey("ParentMessageId")]
        [JsonIgnore]
        public Message? ParentMessage { get; set; }

        /// <summary>
        /// Snippet of parent message for quick display without a join.
        /// </summary>
        [MaxLength(300)]
        public string? ParentContentPreview { get; set; }

        [MaxLength(100)]
        public string? ParentSenderName { get; set; }

        public bool IsPinned { get; set; } = false;

        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When true, Content is AES-GCM encrypted client-side.
        /// The server stores the ciphertext only; never sees plaintext.
        /// </summary>
        public bool IsEncrypted { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? EditedAt { get; set; }
    }
}
