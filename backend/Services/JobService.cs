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
        private readonly IServiceRepository _serviceRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IBookingRepository _bookingRepo;
        private readonly IEscrowRepository _escrowRepo;

        public JobService(IJobRepository jobRepo, IUserRepository userRepo, IAuditLogRepository auditRepo, IServiceRepository serviceRepo, IWalletRepository walletRepo, IBookingRepository bookingRepo, IEscrowRepository escrowRepo)
        {
            _jobRepo = jobRepo;
            _userRepo = userRepo;
            _auditRepo = auditRepo;
            _serviceRepo = serviceRepo;
            _walletRepo = walletRepo;
            _bookingRepo = bookingRepo;
            _escrowRepo = escrowRepo;
        }

        public async Task<JobResponse> CreateJobAsync(Guid creatorId, CreateJobRequest request)
        {
            var user = await _userRepo.GetByIdAsync(creatorId)
                ?? throw new InvalidOperationException("User not found.");

            var job = new Job
            {
                Title = request.Title,
                Description = request.Description,
                Amount = request.Amount,
                CreatorId = creatorId,
                CategoryName = request.CategoryName,
                Status = JobStatus.Open
            };

            await _jobRepo.AddAsync(job);
            await _jobRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                JobId = job.Id,
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
                CategoryName = job.CategoryName,
                ApplicationCount = 0,
                CreatedAt = job.CreatedAt
            };
        }

        public async Task<JobResponse> UpdateJobAsync(Guid creatorId, Guid jobId, UpdateJobRequest request)
        {
            var job = await _jobRepo.GetJobWithCreatorAsync(jobId) 
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

        public async Task DeleteJobAsync(Guid creatorId, Guid jobId)
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
                CategoryName = j.CategoryName,
                ApplicationCount = j.Bookings?.Count(b => b.Status == BookingStatus.Pending) ?? 0,
                CreatedAt = j.CreatedAt
            }).ToList();
        }

        public async Task<JobResponse?> GetJobByIdAsync(Guid jobId)
        {
            var j = await _jobRepo.GetJobWithCreatorAsync(jobId);

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

        public async Task<List<JobResponse>> GetMyRequestsAsync(Guid creatorId)
        {
            var jobs = await _jobRepo.FindAsync(j => j.CreatorId == creatorId);
            // Re-fetch with bookings included
            var detailedJobs = new List<JobResponse>();
            foreach (var j in jobs)
            {
                var bookings = await _bookingRepo.GetByJobIdAsync(j.Id);
                detailedJobs.Add(new JobResponse
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Amount = j.Amount,
                    Status = j.Status.ToString(),
                    CreatorId = j.CreatorId,
                    CreatorName = "Me",
                    CategoryName = j.CategoryName,
                    ApplicationCount = bookings.Count(b => b.Status == BookingStatus.Pending),
                    CreatedAt = j.CreatedAt
                });
            }
            return detailedJobs;
        }

        public async Task<List<JobResponse>> GetMyServiceRequestsAsync(Guid providerId)
        {
            var jobs = await _jobRepo.GetJobsByServiceProviderAsync(providerId);
            return jobs.Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Amount = j.Amount,
                Status = j.Status.ToString(),
                CreatorId = j.CreatorId,
                CreatorName = j.Creator?.Name ?? "Unknown",
                CreatedAt = j.CreatedAt
            }).ToList();
        }

        public async Task<JobResponse> ConfirmCompletionAsync(Guid creatorId, Guid jobId)
        {
            var job = await _jobRepo.GetJobWithCreatorAsync(jobId) ?? throw new KeyNotFoundException("Job not found.");
            
            if (job.CreatorId != creatorId)
                throw new UnauthorizedAccessException("Only the creator can confirm completion.");

            var activeBooking = await _bookingRepo.GetBookingDetailedAsync(job.Id);
            if (activeBooking == null) 
            {
                var list = await _bookingRepo.GetByJobIdAsync(job.Id);
                activeBooking = list.FirstOrDefault(b => b.Status == BookingStatus.Active);
            }

            if (activeBooking != null)
            {
                var escrow = await _escrowRepo.GetByBookingIdAsync(activeBooking.Id);
                if (escrow != null)
                {
                    if (escrow.Status != LocaLe.EscrowApi.Models.EscrowStatus.Released)
                    {
                        throw new InvalidOperationException("You cannot mark the job as completed until the provider has unlocked the vault using the code.");
                    }

                    activeBooking.Status = BookingStatus.Finalized;
                    _bookingRepo.Update(activeBooking);

                    // Add to provider's completed jobs stat here as per user request
                    var provider = await _userRepo.GetByIdAsync(escrow.ProviderId);
                    if (provider != null)
                    {
                        provider.JobsCompleted += 1;
                        _userRepo.Update(provider);
                    }

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        JobId = job.Id,
                        ReferenceType = "Job",
                        ReferenceId = job.Id,
                        Action = "COMPLETED",
                        ActorId = creatorId,
                        Details = "Job successfully closed by buyer after vault release."
                    });
                }
            }

            await _jobRepo.SaveChangesAsync();
            await _escrowRepo.SaveChangesAsync();
            await _walletRepo.SaveChangesAsync();

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

        public async Task<List<BookingResponse>> GetApplicantsForJobAsync(Guid jobId, Guid requesterId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId)
                ?? throw new KeyNotFoundException("Job not found.");

            if (job.CreatorId != requesterId)
                throw new UnauthorizedAccessException("Only the job creator can view applicants.");

            var bookings = await _bookingRepo.GetPendingApplicationsForJobAsync(jobId);
            return bookings.Select(b => new BookingResponse
            {
                Id = b.Id,
                JobId = b.JobId,
                JobTitle = job.Title,
                ProviderId = b.ProviderId,
                ProviderName = b.Provider?.Name ?? "Unknown",
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt
            }).ToList();
        }

        public async Task<JobResponse> CreateJobForServiceAsync(Guid creatorId, Guid serviceId, CreateJobRequest request)
        {
            var user = await _userRepo.GetByIdAsync(creatorId) ?? throw new InvalidOperationException("User not found.");
            
            var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new KeyNotFoundException($"Service with ID {serviceId} does not exist. It may have been deleted.");

            var wallet = await _walletRepo.GetByUserIdAsync(creatorId) 
                ?? throw new InvalidOperationException("User wallet not found.");

            if (wallet.Balance < request.Amount)
                throw new InvalidOperationException($"Insufficient funds to request this service. Required: ₦{request.Amount:N2}, Available: ₦{wallet.Balance:N2}.");

            var job = new Job
            {
                Title = request.Title,
                Description = request.Description,
                Amount = request.Amount,
                CreatorId = creatorId,
                ServiceId = serviceId,
                Status = JobStatus.Open
            };

            await _jobRepo.AddAsync(job);
            await _jobRepo.SaveChangesAsync();

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
    }
}
