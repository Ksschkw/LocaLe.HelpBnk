using LocaLe.EscrowApi.DTOs;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAuditLogRepository _auditRepo;

        public JobService(IJobRepository jobRepo, IUserRepository userRepo, IAuditLogRepository auditRepo)
        {
            _jobRepo = jobRepo;
            _userRepo = userRepo;
            _auditRepo = auditRepo;
        }

        public async Task<JobResponse> CreateJobAsync(int creatorId, CreateJobRequest request)
        {
            var user = await _userRepo.GetByIdAsync(creatorId)
                ?? throw new InvalidOperationException("User not found.");

            var job = new Job
            {
                Title = request.Title,
                Description = request.Description,
                Amount = request.Amount,
                CreatorId = creatorId,
                Status = JobStatus.Open
            };

            await _jobRepo.AddAsync(job);
            await _jobRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Job",
                ReferenceId = job.Id,
                Action = "CREATED",
                ActorId = creatorId,
                Details = $"Job '{request.Title}' posted for ₦{request.Amount:N2}."
            });
            await _auditRepo.SaveChangesAsync();

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

        public async Task<JobResponse> UpdateJobAsync(int creatorId, int jobId, UpdateJobRequest request)
        {
            var job = await _jobRepo.GetJobDetailedAsync(jobId) 
                ?? throw new KeyNotFoundException("Job not found.");

            if (job.CreatorId != creatorId)
                throw new UnauthorizedAccessException("You can only edit your own jobs.");

            if (job.Status != JobStatus.Open)
                throw new InvalidOperationException("You can only edit jobs that are Open (not under Escrow/Review).");

            if (request.Title != null) job.Title = request.Title;
            if (request.Description != null) job.Description = request.Description;
            if (request.Amount.HasValue) job.Amount = request.Amount.Value;
            
            if (request.Status != null && Enum.TryParse<JobStatus>(request.Status, true, out var parsedStatus))
                job.Status = parsedStatus;

            _jobRepo.Update(job);
            await _jobRepo.SaveChangesAsync();

            return new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Amount = job.Amount,
                Status = job.Status.ToString(),
                CreatorId = job.CreatorId,
                CreatorName = job.Creator?.Name ?? "Unknown",
                CreatedAt = job.CreatedAt
            };
        }

        public async Task DeleteJobAsync(int creatorId, int jobId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId) 
                ?? throw new KeyNotFoundException("Job not found.");

            if (job.CreatorId != creatorId)
                throw new UnauthorizedAccessException("You can only delete your own jobs.");

            if (job.Status != JobStatus.Open && job.Status != JobStatus.Completed)
                throw new InvalidOperationException("You cannot delete a job that is actively assigned or in dispute.");

            _jobRepo.Remove(job);
            await _jobRepo.SaveChangesAsync();
        }

        public async Task<List<JobResponse>> GetOpenJobsAsync()
        {
            var jobs = await _jobRepo.GetAllOpenJobsAsync();
            
            return jobs.Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Amount = j.Amount,
                Status = j.Status.ToString(),
                CreatorId = j.CreatorId,
                CreatorName = j.Creator != null ? j.Creator.Name : "Unknown",
                CreatedAt = j.CreatedAt
            }).ToList();
        }

        public async Task<JobResponse?> GetJobByIdAsync(int jobId)
        {
            var j = await _jobRepo.GetJobDetailedAsync(jobId);

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
