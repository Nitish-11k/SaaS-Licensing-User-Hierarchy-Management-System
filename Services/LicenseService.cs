using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Services
{
    public class LicenseService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public LicenseService(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<License> CreateLicenseAsync(Guid tenantId, CreateLicenseRequest request)
        {
            var license = new License
            {
                Name = request.Name,
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

            // Check if already assigned (Concurrency/Double booking check)
            var currentCount = await _context.LicenseAssignments.CountAsync(la => la.LicenseId == license.Id);
            if (currentCount >= license.MaxSeats) throw new Exception("License seat limit reached.");

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

            // 2. Log Action
            await _auditService.LogActionAsync(tenantId, adminId, "Assign License", $"License {license.LicenseKey} assigned to User {targetUser.Email}");
        }

        public async Task RevokeLicenseAsync(Guid adminId, Guid tenantId, Guid licenseId)
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId && l.TenantId == tenantId);
            if (license == null) throw new Exception("License not found.");

            license.Status = LicenseStatus.Revoked;
            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Revoke License", $"License {license.LicenseKey} revoked.");
        }

        public async Task TransferLicenseAsync(Guid adminId, Guid tenantId, Guid assignmentId, Guid newUserId)
        {
            var assignment = await _context.LicenseAssignments
                .Include(la => la.License)
                .FirstOrDefaultAsync(la => la.Id == assignmentId && la.TenantId == tenantId);

            if (assignment == null) throw new Exception("Assignment not found.");

            var newUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == newUserId && u.TenantId == tenantId);
            if (newUser == null) throw new Exception("Target user not found.");

            var oldUserId = assignment.UserId;
            assignment.UserId = newUserId; 
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Transfer License", $"License {assignment.License.LicenseKey} transferred from {oldUserId} to {newUser.Email}");
        }

        public async Task<MachineValidationResponse> ValidateMachineAsync(Guid userId, MachineHeartbeatRequest request)
        {
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

     public async Task<List<MachineDto>> GetMachinesByLicenseIdAsync(Guid tenantId, Guid licenseId)
    {

        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId && l.TenantId == tenantId);
        if (license == null) throw new Exception("License not found.");
       var machines = await _context.Machines
        .Include(m => m.LicenseAssignment)
        .ThenInclude(la => la.User)
        .Where(m => m.LicenseAssignment.LicenseId == licenseId)
        .Select(m => new MachineDto(
            m.Id,
            m.HardwareId,
            m.MachineName,
            m.OperatingSystem,
            m.IpAddress,
            m.LastActive,
            m.LicenseAssignment.User.Email
        ))
            .ToListAsync();

        return machines;
    }

    public async Task<List<MachineDto>> GetMachinesByUserIdAsync(Guid tenantId, Guid userId)
    {
       var machines = await _context.Machines
        .Include(m => m.LicenseAssignment)
        .ThenInclude(la => la.User)
        .Where(m => m.LicenseAssignment.UserId == userId && m.TenantId == tenantId)
        .Select(m => new MachineDto(
            m.Id,
            m.HardwareId,
            m.MachineName,
            m.OperatingSystem,
            m.IpAddress,
            m.LastActive,
            m.LicenseAssignment.User.Email
        ))
            .ToListAsync();

           return machines;
        }
    public async Task<LicenseUsageStats> GetLicenseUsageAsync(Guid tenantId)
    {
        var totalLicenses = await _context.Licenses.CountAsync();
        var activeLicenses = await _context.Licenses.CountAsync(l => l.Status == LicenseStatus.Active);
    
        var licenseData = await _context.Licenses
            .Include(l => l.Assignments)
            .Select(l => new 
        {
            l.Name,
            l.MaxSeats,
            UsedSeats = l.Assignments.Count
        }).ToListAsync();

        return new LicenseUsageStats(
            totalLicenses, 
            activeLicenses, 
            licenseData.Sum(x => x.MaxSeats), 
            licenseData.Sum(x => x.UsedSeats)
        );
    }

    public async Task UpgradeLicenseAsync(Guid adminId, Guid tenantId, Guid licenseId, int addedSeats, int addedDays)
    {
        var license = await _context.Licenses.FindAsync(licenseId);
        if (license == null) throw new Exception("License not found.");

        license.MaxSeats += addedSeats;
        license.ExpiryDate = license.ExpiryDate.AddDays(addedDays);
    
        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(tenantId, adminId, "Upgrade License", $"Added {addedSeats} seats and {addedDays} days.");
    }
    }
}