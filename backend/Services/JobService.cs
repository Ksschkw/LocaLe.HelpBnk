using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Services
{
    public class JobService : IJobService
    {
        private readonly EscrowContext _context;

        public JobService(EscrowContext context)
        {
            _context = context;
        }

        public async Task<JobResponse> CreateJobAsync(int creatorId, CreateJobRequest request)
        {
            var user = await _context.Users.FindAsync(creatorId)
                ?? throw new InvalidOperationException("User not found.");

            var job = new Job
            {
                Title = request.Title,
                Description = request.Description,
                Amount = request.Amount,
                CreatorId = creatorId,
                Status = JobStatus.Open
            };

            _context.Jobs.Add(job);

            _context.AuditLogs.Add(new AuditLog
            {
                ReferenceType = "Job",
                ReferenceId = 0, // Will be updated after SaveChanges
                Action = "CREATED",
                ActorId = creatorId,
                Details = $"Job '{request.Title}' posted for ₦{request.Amount:N2}."
            });

            await _context.SaveChangesAsync();

            // Patch the audit log ReferenceId now that we have the real Job ID
            var lastLog = await _context.AuditLogs
                .OrderByDescending(a => a.Id)
                .FirstAsync(a => a.Action == "CREATED" && a.ActorId == creatorId);
            lastLog.ReferenceId = job.Id;
            await _context.SaveChangesAsync();

            return new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Amount = job.Amount,
                Status = job.Status.ToString(),
                CreatorId = job.CreatorId,
                CreatorName = user.Name,
                CreatedAt = job.CreatedAt
            };
        }

        public async Task<List<JobResponse>> GetOpenJobsAsync()
        {
            return await _context.Jobs
                .Where(j => j.Status == JobStatus.Open)
                .Include(j => j.Creator)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new JobResponse
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Amount = j.Amount,
                    Status = j.Status.ToString(),
                    CreatorId = j.CreatorId,
                    CreatorName = j.Creator != null ? j.Creator.Name : "Unknown",
                    CreatedAt = j.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<JobResponse?> GetJobByIdAsync(int jobId)
        {
            var j = await _context.Jobs
                .Include(j => j.Creator)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (j == null) return null;

            return new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Amount = j.Amount,
                Status = j.Status.ToString(),
                CreatorId = j.CreatorId,
                CreatorName = j.Creator?.Name ?? "Unknown",
                CreatedAt = j.CreatedAt
            };
        }
    }
}
