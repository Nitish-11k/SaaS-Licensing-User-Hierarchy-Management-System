using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Services
{
    public class LicenseService
    {
        private readonly AppDbContext _context;

        public LicenseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<License> CreateLicenseAsync(Guid tenantId, CreateLicenseRequest request)
        {
            var license = new License
            {
                LicenseKey = Guid.NewGuid().ToString("N").ToUpper(), 
                Type = request.Type,
                MaxSeats = request.MaxSeats,
                Status = LicenseStatus.Active,
                StartDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(request.DurationInDays),
                TenantId = tenantId
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();
            return license;
        }

        public async Task AssignLicenseAsync(Guid adminId, Guid tenantId, AssignLicenseRequest request)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Id == request.LicenseId && l.TenantId == tenantId);
            
            if (license == null) throw new Exception("License not found or access denied.");
            if (license.Status != LicenseStatus.Active) throw new Exception("License is not active.");

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.TenantId == tenantId);
            
            if (targetUser == null) throw new Exception("User not found.");
            var assignment = new LicenseAssignment
            {
                LicenseId = license.Id,
                UserId = targetUser.Id,
                TenantId = tenantId
            };

            _context.LicenseAssignments.Add(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task<MachineValidationResponse> ValidateMachineAsync(Guid userId, MachineHeartbeatRequest request)
        {
            // 1. Find User's Active License Assignment
            var assignment = await _context.LicenseAssignments
                .Include(la => la.License)
                .Include(la => la.Machines)
                .Where(la => la.UserId == userId)
                .OrderByDescending(la => la.CreatedAt) 
                .FirstOrDefaultAsync();

            if (assignment == null) 
                return new MachineValidationResponse(false, "No license assigned to this user.");

            if (assignment.License.ExpiryDate < DateTime.UtcNow)
                return new MachineValidationResponse(false, "License has expired.");

            var existingMachine = assignment.Machines.FirstOrDefault(m => m.HardwareId == request.HardwareId);

            if (existingMachine != null)
            {
                existingMachine.LastActive = DateTime.UtcNow;
                existingMachine.IpAddress = request.IpAddress;
                existingMachine.MachineName = request.MachineName; 
                await _context.SaveChangesAsync();
                return new MachineValidationResponse(true, "Validated successfully.");
            }

            if (assignment.Machines.Count >= assignment.License.MaxSeats)
            {
                return new MachineValidationResponse(false, $"Seat limit reached. ({assignment.Machines.Count}/{assignment.License.MaxSeats} used). Unbind an old machine first.");
            }

            var newMachine = new Machine
            {
                HardwareId = request.HardwareId,
                MachineName = request.MachineName,
                OperatingSystem = request.OperatingSystem,
                IpAddress = request.IpAddress,
                LastActive = DateTime.UtcNow,
                LicenseAssignmentId = assignment.Id,
                TenantId = assignment.License.TenantId
            };

            _context.Machines.Add(newMachine);
            await _context.SaveChangesAsync();

            return new MachineValidationResponse(true, "Machine registered and validated.");
        }
    }
}