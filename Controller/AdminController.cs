using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; 
using SaasLicenseSystem.Api.Services;
using System.Security.Claims;
using SaasLicenseSystem.Api.Services;

namespace SaasLicenseSystem.Api.Controllers{

[Route("api/admins")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly UserService _userService;

    public AdminController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteAdmin([FromBody] string email)
    {
        try
        {
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            
            // Reusing the generic invite logic but forcing Role="Admin"
            var token = await _userService.InviteUserAsync(adminId, tenantId, email, "Admin");
            return Ok(new { message = "Admin invited", token });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
}