namespace SaasLicenseSystem.Api.DTOs
{
    public record RegisterTenantRequest(string OrganizationName, string OwnerName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string RefreshToken, string Role, Guid TenantId);
}