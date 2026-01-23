using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Entities;
using BCrypt.Net;
using SaasLicenseSystem.Api.Services;

namespace SaasLicenseSystem.Api.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public UserService(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<UserResponse> CreateUserAsync(Guid creatorId, Guid tenantId, CreateUserRequest request)
        {
            var creator = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == creatorId);
            if (creator == null) throw new Exception("Creator not found.");

            if (!CanCreate(creator.Role.Name, request.RoleName))
            {
                throw new Exception($"Role '{creator.Role.Name}' cannot create '{request.RoleName}'.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName && (r.TenantId == tenantId || r.TenantId == Guid.Empty)); 
            if (role == null) throw new Exception("Invalid Role.");

            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = role.Id,
                TenantId = tenantId,
                ParentId = creator.Id, 
                DepartmentId = request.DepartmentId,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            if (request.GroupIds != null && request.GroupIds.Any())
            {
                foreach(var groupId in request.GroupIds)
                {
                    _context.UserGroups.Add(new UserGroup { UserId = newUser.Id, GroupId = groupId });
                }
                await _context.SaveChangesAsync();
            }

            return new UserResponse(newUser.Id, newUser.FullName, newUser.Email, request.RoleName, null, newUser.ParentId, newUser.IsActive);
        }

        public async Task<string> InviteUserAsync(Guid inviterId, Guid tenantId, string email, string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) throw new Exception("Role not found.");

            var user = new User
        {
            Email = email,
            FullName = "Pending Invitation",
            PasswordHash = "", 
            RoleId = role.Id,
            TenantId = tenantId,
            ParentId = inviterId,
            IsActive = false 
        };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

    
            await _auditService.LogActionAsync(tenantId, inviterId, "Invite User", $"Invited {email} as {roleName}");

    
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{Guid.NewGuid()}")); 
        }

        public async Task UpdateDepartmentAsync(Guid adminId, Guid tenantId, Guid targetUserId, Guid departmentId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (user == null) throw new Exception("User not found.");

            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
            if (department == null) throw new Exception("Department not found.");

            user.DepartmentId = departmentId;
            await _context.SaveChangesAsync();
 
            await _auditService.LogActionAsync(tenantId, adminId, "Update Department", $"Moved {user.Email} to {department.Name}");
        }

        public async Task<List<UserResponse>> GetHierarchyTreeAsync(Guid userId)
        {
                  
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .Where(u => u.ParentId == userId)
                .ToListAsync();

            return users.Select(u => new UserResponse(
                u.Id, 
                u.FullName, 
                u.Email, 
                u.Role.Name, 
                u.Department?.Name, 
                u.ParentId, 
                u.IsActive
            )).ToList();
        }

        public async Task UpdateUserRoleAsync(Guid adminId, Guid tenantId, Guid userId, string newRoleName)
        {
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
            if (targetUser == null) throw new Exception("User not found.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == newRoleName && r.TenantId == tenantId);
            if (role == null) throw new Exception("Role not found.");

            targetUser.RoleId = role.Id;
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditService.LogActionAsync(tenantId, adminId, "Update Role", $"User {targetUser.Email} role changed to {newRoleName}");
        }

        private bool CanCreate(string creatorRole, string targetRole)
        {
            if (creatorRole == "SuperAdmin") return targetRole == "Admin";
            if (creatorRole == "Admin") return targetRole == "Manager";
            if (creatorRole == "Manager") return targetRole == "User";
            return false;
        }

    }
}