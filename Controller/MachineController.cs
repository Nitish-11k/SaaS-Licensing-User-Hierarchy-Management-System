using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasLicenseSystem.Api.Services;
using System.Security.Claims;

namespace SaasLicenseSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MachinesController : ControllerBase
    {
        private readonly LicenseService _licenseService;

        public MachinesController(LicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        [HttpGet("by-license/{licenseId}")]
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        public async Task<IActionResult> GetMachinesByLicense(Guid licenseId)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var result = await _licenseService.GetMachinesByLicenseIdAsync(tenantId, licenseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-user/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        public async Task<IActionResult> GetMachinesByUser(Guid userId)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var result = await _licenseService.GetMachinesByUserIdAsync(tenantId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}