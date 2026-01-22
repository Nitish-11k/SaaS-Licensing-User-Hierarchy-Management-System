namespace SaasLicenseSystem.Api.DTOs
{
    public record CreateDepartmentRequest(string Name);
    public record DepartmentResponse(Guid Id, string Name, DateTime CreatedAt);

    public record CreateGroupRequest(string Name);
    public record GroupResponse(Guid Id, string Name, DateTime CreatedAt);
}