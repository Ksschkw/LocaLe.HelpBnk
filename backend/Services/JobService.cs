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

            var wallet = await _walletRepo.GetByUserIdAsync(creatorId) 
                ?? throw new InvalidOperationException("User wallet not found.");

            if (wallet.Balance < request.Amount)
                throw new InvalidOperationException($"Insufficient funds to post this job. Required: ₦{request.Amount:N2}, Available: ₦{wallet.Balance:N2}.");

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
            return jobs.Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Amount = j.Amount,
                Status = j.Status.ToString(),
                CreatorId = j.CreatorId,
                CreatorName = "Me",
                CreatedAt = j.CreatedAt
            }).ToList();
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

            job.Status = JobStatus.Completed;
            _jobRepo.Update(job);

            var activeBooking = await _bookingRepo.GetBookingDetailedAsync(job.Id); // Uses GetBookingDetailedAsync (some repos use this for active booking)
            if (activeBooking == null) 
            {
                // Try finding directly if there's a custom method, fallback to manual list
                var list = await _bookingRepo.GetByJobIdAsync(job.Id);
                activeBooking = list.FirstOrDefault(b => b.Status == BookingStatus.Active);
            }

            if (activeBooking != null)
            {
                activeBooking.Status = BookingStatus.Finalized;
                _bookingRepo.Update(activeBooking);

                var escrow = await _escrowRepo.GetByBookingIdAsync(activeBooking.Id);
                if (escrow != null && escrow.Status == LocaLe.EscrowApi.Models.EscrowStatus.Secured)
                {
                    var providerWallet = await _walletRepo.GetByUserIdAsync(escrow.ProviderId) ?? throw new InvalidOperationException("Provider wallet not found.");
                    providerWallet.Balance += escrow.Amount;
                    _walletRepo.Update(providerWallet);

                    escrow.Status = LocaLe.EscrowApi.Models.EscrowStatus.Released;
                    escrow.QrToken = null;
                    escrow.QrTokenExpiry = null;
                    _escrowRepo.Update(escrow);

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        JobId = job.Id,
                        ReferenceType = "Escrow",
                        ReferenceId = escrow.Id,
                        Action = "REMOTE_RELEASE",
                        ActorId = creatorId,
                        Details = $"Job marked complete by Buyer. ₦{escrow.Amount:N2} released to provider."
                    });
                }
            }

            await _jobRepo.SaveChangesAsync();
            await _escrowRepo.SaveChangesAsync(); // save escrow updates
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
