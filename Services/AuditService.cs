using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.Entities;
using SaasLicenseSystem.Api.DTOs;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<AuditLogResponse>> GetAuditLogsAsync(Guid tenantId)
        {
            var query = from log in _context.AuditLogs
                        join user in _context.Users on log.PerformedByUserId equals user.Id
                        where log.TenantId == tenantId
                        orderby log.CreatedAt descending
                        select new AuditLogResponse(
                            log.Id,
                            log.Action,
                            log.EntityAffected,
                            log.Details,
                            user.Email,
                            log.CreatedAt
                        );

            return await query.ToListAsync();
        }
    }
}