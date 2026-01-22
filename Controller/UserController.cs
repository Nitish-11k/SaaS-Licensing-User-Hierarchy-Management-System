using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Services;
using System.Security.Claims;

namespace SaasLicenseSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);

                var result = await _userService.CreateUserAsync(userId, tenantId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetMyHierarchy()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _userService.GetHierarchyTreeAsync(userId);
            return Ok(result);
        }

        [HttpPatch("{id}/role")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] string newRoleName)
        {
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            
            await _userService.UpdateUserRoleAsync(adminId, tenantId, id, newRoleName);
            return Ok(new { message = "Role updated." });
        }
    }
}