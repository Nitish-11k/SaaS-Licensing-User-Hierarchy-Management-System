namespace SaasLicenseSystem.Api.DTOs
{
    public record RegisterTenantRequest(string OrganizationName, string OwnerName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string RefreshToken, string Role, Guid TenantId);
    
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);


    public record AuditLogResponse(
        Guid Id,
        string Action,
        string EntityAffected,
        string? Details,
        string PerformedBy,
        DateTime Timestamp
    );

}