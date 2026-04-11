using LocaLe.EscrowApi.Interfaces.Repositories;
using LocaLe.EscrowApi.Models;

namespace LocaLe.EscrowApi.Services
{
    public interface IVouchService
    {
        Task VouchAsync(Guid serviceId, Guid voucherId, string? comment);
        Task GuestVouchAsync(Guid serviceId, string phone, string name, string ip, string userAgent, string? comment);
        Task<(int Total, int Platform, int Guest)> GetServicePointsBreakdownAsync(Guid serviceId);
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

            int points = 5; // Platform user weight
            
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
            
            if (service.TrustPoints >= service.RequiredVouchPoints && !service.IsDiscoveryAdminOverridden)
            {
                service.IsDiscoveryEnabled = true;
            }

            _serviceRepo.Update(service);

            // Update user total reputation
            var provider = await _userRepo.GetByIdAsync(service.ProviderId);
            if (provider != null)
            {
                provider.TotalVouchPoints += points;
                UpdateUserTrustAndTier(provider);
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

        public async Task GuestVouchAsync(Guid serviceId, string phone, string name, string ip, string userAgent, string? comment)
        {
            var service = await _serviceRepo.GetByIdAsync(serviceId)
                ?? throw new KeyNotFoundException("Service not found.");

            // Deduplication: Check if this phone number has already vouched for this service
            var existingGuestVouch = await _vouchRepo.FindAsync(v => v.ServiceId == serviceId && v.GuestPhone == phone);
            if (existingGuestVouch.Any())
                throw new InvalidOperationException("You have already vouched for this service using this phone number.");

            int points = 1; // Guest user weight
            
            var vouch = new Vouch
            {
                ServiceId = serviceId,
                GuestPhone = phone,
                GuestName = name,
                GuestIpAddress = ip,
                GuestUserAgent = userAgent,
                PointsGiven = points,
                Comment = comment,
                IsPlatformUser = false
            };

            await _vouchRepo.AddAsync(vouch);
            
            service.TrustPoints += points;
            if (service.TrustPoints >= service.RequiredVouchPoints && !service.IsDiscoveryAdminOverridden)
            {
                service.IsDiscoveryEnabled = true;
            }
            _serviceRepo.Update(service);

            var provider = await _userRepo.GetByIdAsync(service.ProviderId);
            if (provider != null)
            {
                provider.TotalVouchPoints += points;
                UpdateUserTrustAndTier(provider);
                _userRepo.Update(provider);
            }

            await _vouchRepo.SaveChangesAsync();
            await _serviceRepo.SaveChangesAsync();
            await _userRepo.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                ReferenceType = "Service",
                ReferenceId = serviceId,
                Action = "GuestVouched",
                ActorId = Guid.Empty, // Guest
                Details = $"Guest {name} ({phone}) vouched for service. Discovery enabled: {service.IsDiscoveryEnabled}."
            });
            await _auditRepo.SaveChangesAsync();
        }

        public async Task<(int Total, int Platform, int Guest)> GetServicePointsBreakdownAsync(Guid serviceId)
        {
            var vouches = await _vouchRepo.FindAsync(v => v.ServiceId == serviceId && !v.IsRetracted);
            int platform = vouches.Where(v => v.IsPlatformUser).Sum(v => v.PointsGiven);
            int guest = vouches.Where(v => !v.IsPlatformUser).Sum(v => v.PointsGiven);
            return (platform + guest, platform, guest);
        }

        private void UpdateUserTrustAndTier(User user)
        {
            int avgVouchPerJob = (user.JobsCompleted == 0) ? 0 : (user.TotalVouchPoints / user.JobsCompleted);
            user.TrustScore = user.TotalVouchPoints + (avgVouchPerJob * 10) + (user.JobsCompleted * 20);

            if (user.TrustScore >= 1000) user.Tier = UserTier.Platinum;
            else if (user.TrustScore >= 250) user.Tier = UserTier.Gold;
            else if (user.TrustScore >= 50) user.Tier = UserTier.Silver;
            else user.Tier = UserTier.Bronze;
        }
    }
}
