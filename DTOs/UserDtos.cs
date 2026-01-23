namespace SaasLicenseSystem.Api.DTOs
{
    public record CreateUserRequest(
        string FullName, 
        string Email, 
        string Password, 
        string RoleName,
        Guid? DepartmentId,
        List<Guid>? GroupIds
    );

    public record InviteUserRequest(string Email, string RoleName);

    public record UserResponse(
        Guid Id, 
        string FullName, 
        string Email, 
        string Role, 
        string? Department,
        Guid? ParentId,
        bool IsActive
    );
}