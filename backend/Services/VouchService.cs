using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public interface IVouchService
    {
        Task VouchAsync(Guid serviceId, Guid voucherId, string? comment);
        Task<int> GetServicePointsAsync(Guid serviceId);
    }

    public class VouchService : IVouchService
    {
        private readonly IVouchRepository _vouchRepo;
        private readonly IServiceRepository _serviceRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAuditLogRepository _auditRepo;

        public VouchService(
            IVouchRepository vouchRepo,
            IServiceRepository serviceRepo,
            IUserRepository userRepo,
            IAuditLogRepository auditRepo)
        {
            _vouchRepo = vouchRepo;
            _serviceRepo = serviceRepo;
            _userRepo = userRepo;
            _auditRepo = auditRepo;
        }

        public async Task VouchAsync(Guid serviceId, Guid voucherId, string? comment)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId)
                ?? throw new KeyNotFoundException("Service not found.");

            var voucher = await _userRepo.GetByIdAsync(voucherId)
                ?? throw new KeyNotFoundException("Voucher user not found.");

            if (service.ProviderId == voucherId)
                throw new InvalidOperationException("You cannot vouch for your own service.");

            var alreadyVouched = await _vouchRepo.HasUserVouchedForServiceAsync(voucherId, serviceId);
            if (alreadyVouched)
                throw new InvalidOperationException("You have already vouched for this service.");

            int points = 10; // Platform user weight
            
            var vouch = new Vouch
            {
                ServiceId = serviceId,
                VoucherId = voucherId,
                PointsGiven = points,
                Comment = comment,
                IsPlatformUser = true
            };

            await _vouchRepo.AddAsync(vouch);
            
            // Update service cache/denormalized points
            service.TrustPoints += points;
            
            if (service.TrustPoints >= service.RequiredVouchPoints)
            {
                service.IsDiscoveryEnabled = true;
            }

            _serviceRepo.Update(service);

            // Update user total reputation
            var provider = await _userRepo.GetByIdAsync(service.ProviderId);
            if (provider != null)
            {
                provider.TotalVouchPoints += points;
                // Tier logic
                if (provider.TotalVouchPoints > 1000) provider.Tier = UserTier.Platinum;
                else if (provider.TotalVouchPoints > 500) provider.Tier = UserTier.Gold;
                else if (provider.TotalVouchPoints > 100) provider.Tier = UserTier.Silver;

                _userRepo.Update(provider);
            }

            await _vouchRepo.SaveChangesAsync();
            await _serviceRepo.SaveChangesAsync();
            await _userRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Service",
                ReferenceId = serviceId,
                Action = "Vouced",
                ActorId = voucherId,
                Details = $"User {voucher.Name} vouched for service. Discovery enabled: {service.IsDiscoveryEnabled}."
            });
            await _auditRepo.SaveChangesAsync();
        }

        public async Task<int> GetServicePointsAsync(Guid serviceId)
        {
            return await _vouchRepo.GetTotalPointsForServiceAsync(serviceId);
        }
    }
}
