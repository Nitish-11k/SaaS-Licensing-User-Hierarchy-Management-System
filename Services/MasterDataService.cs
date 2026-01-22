using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Services
{
    public class MasterDataService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public MasterDataService(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }


        public async Task<DepartmentResponse> CreateDepartmentAsync(Guid tenantId, Guid adminId, string name)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == name && d.TenantId == tenantId))
                throw new Exception("Department already exists.");

            var dept = new Department { Name = name, TenantId = tenantId };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Create Department", $"Created department '{name}'");

            return new DepartmentResponse(dept.Id, dept.Name, dept.CreatedAt);
        }

        public async Task<List<DepartmentResponse>> GetDepartmentsAsync(Guid tenantId)
        {
            return await _context.Departments
                .Where(d => d.TenantId == tenantId)
                .Select(d => new DepartmentResponse(d.Id, d.Name, d.CreatedAt))
                .ToListAsync();
        }

        public async Task DeleteDepartmentAsync(Guid tenantId, Guid adminId, Guid id)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
            if (dept == null) throw new Exception("Department not found.");

            var hasUsers = await _context.Users.AnyAsync(u => u.DepartmentId == id);
            if (hasUsers) throw new Exception("Cannot delete department with assigned users.");

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Delete Department", $"Deleted department '{dept.Name}'");
        }


        public async Task<GroupResponse> CreateGroupAsync(Guid tenantId, Guid adminId, string name)
        {
            if (await _context.Groups.AnyAsync(g => g.Name == name && g.TenantId == tenantId))
                throw new Exception("Group already exists.");

            var group = new Group { Name = name, TenantId = tenantId };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Create Group", $"Created group '{name}'");

            return new GroupResponse(group.Id, group.Name, group.CreatedAt);
        }

        public async Task<List<GroupResponse>> GetGroupsAsync(Guid tenantId)
        {
            return await _context.Groups
                .Where(g => g.TenantId == tenantId)
                .Select(g => new GroupResponse(g.Id, g.Name, g.CreatedAt))
                .ToListAsync();
        }

        public async Task DeleteGroupAsync(Guid tenantId, Guid adminId, Guid id)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId);
            if (group == null) throw new Exception("Group not found.");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(tenantId, adminId, "Delete Group", $"Deleted group '{group.Name}'");
        }
    }
}