using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Data
{
    public class EscrowContext : DbContext
    {
        public EscrowContext(DbContextOptions<EscrowContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Escrow> Escrows => Set<Escrow>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Waitlist> Waitlists => Set<Waitlist>();
        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
        
        // Phase 2: Catalog & Community
        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Vouch> Vouches => Set<Vouch>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Dispute> Disputes => Set<Dispute>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── User ────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ─── Wallet ──────────────────────────────────────
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasOne(w => w.User)
                      .WithOne(u => u.Wallet)
                      .HasForeignKey<Wallet>(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Job ─────────────────────────────────────────
            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasOne(j => j.Creator)
                      .WithMany()
                      .HasForeignKey(j => j.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(j => j.Service)
                      .WithMany()
                      .HasForeignKey(j => j.ServiceId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── Booking ─────────────────────────────────────
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Job)
                      .WithMany()
                      .HasForeignKey(b => b.JobId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Provider)
                      .WithMany()
                      .HasForeignKey(b => b.ProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Escrow ──────────────────────────────────────
            modelBuilder.Entity<Escrow>(entity =>
            {
                entity.HasIndex(e => e.BookingId).IsUnique();
                entity.HasOne(e => e.Booking)
                      .WithOne()
                      .HasForeignKey<Escrow>(e => e.BookingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Phase 2 Models ──────────────────────────────
            modelBuilder.Entity<ServiceCategory>(entity =>
            {
                entity.HasOne(c => c.Parent)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasOne(s => s.Provider)
                      .WithMany(u => u.Services)
                      .HasForeignKey(s => s.ProviderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Category)
                      .WithMany(c => c.Services)
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Reviewer)
                      .WithMany()
                      .HasForeignKey(r => r.ReviewerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewee)
                      .WithMany()
                      .HasForeignKey(r => r.RevieweeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Job)
                      .WithMany()
                      .HasForeignKey(r => r.JobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Vouch>(entity =>
            {
                entity.HasOne(v => v.Voucher)
                      .WithMany()
                      .HasForeignKey(v => v.VoucherId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Service)
                      .WithMany()
                      .HasForeignKey(v => v.ServiceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Job)
                      .WithMany()
                      .HasForeignKey(m => m.JobId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing FK for reply threading
                entity.HasOne(m => m.ParentMessage)
                      .WithMany()
                      .HasForeignKey(m => m.ParentMessageId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(m => m.IsPinned);
                entity.HasIndex(m => new { m.JobId, m.SentAt });
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });


            modelBuilder.Entity<Dispute>(entity =>
            {
                entity.HasOne(d => d.RaisedBy)
                      .WithMany()
                      .HasForeignKey(d => d.RaisedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Job)
                      .WithMany()
                      .HasForeignKey(d => d.JobId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // ─── AuditLog ────────────────────────────────────
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.ReferenceType, a.ReferenceId });
            });

            // ─── Waitlist ────────────────────────────────────
            modelBuilder.Entity<Waitlist>(entity =>
            {
                entity.HasOne(w => w.Service)
                      .WithMany()
                      .HasForeignKey(w => w.ServiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.User)
                      .WithMany()
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
