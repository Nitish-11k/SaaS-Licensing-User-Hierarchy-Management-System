using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Services;
using System.Security.Claims;

namespace SaasLicenseSystem.Api.Controllers
{
    [Route("api")] 
    [ApiController]
    [Authorize]
    public class MasterDataController : ControllerBase
    {
        private readonly MasterDataService _masterDataService;

        public MasterDataController(MasterDataService masterDataService)
        {
            _masterDataService = masterDataService;
        }


        [HttpPost("departments")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var result = await _masterDataService.CreateDepartmentAsync(tenantId, adminId, request.Name);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            var result = await _masterDataService.GetDepartmentsAsync(tenantId);
            return Ok(result);
        }

        [HttpDelete("departments/{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _masterDataService.DeleteDepartmentAsync(tenantId, adminId, id);
                return Ok(new { message = "Department deleted successfully." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("groups")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var result = await _masterDataService.CreateGroupAsync(tenantId, adminId, request.Name);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            var result = await _masterDataService.GetGroupsAsync(tenantId);
            return Ok(result);
        }

        [HttpDelete("groups/{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            try
            {
                var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _masterDataService.DeleteGroupAsync(tenantId, adminId, id);
                return Ok(new { message = "Group deleted successfully." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}