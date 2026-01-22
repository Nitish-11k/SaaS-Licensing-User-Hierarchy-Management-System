using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(Guid tenantId, Guid userId, string action, string entityAffected, string? details = null)
        {
            var log = new AuditLog
            {
                TenantId = tenantId,
                PerformedByUserId = userId,
                Action = action,
                EntityAffected = entityAffected,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}