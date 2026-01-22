using System.IdentityModel.Tokens.Jwt; 
using Microsoft.IdentityModel.Tokens;
using SaasLicenseSystem.Api.Data;
using SaasLicenseSystem.Api.DTOs;
using SaasLicenseSystem.Api.Entities;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace SaasLicenseSystem.Api.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterTenantAsync(RegisterTenantRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var tenant = new Tenant
                {
                    Name = request.OrganizationName,
                    OwnerEmail = request.Email
                };
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                var roles = new List<Role>
                {
                    new Role { Name = "SuperAdmin", Description = "Organization Owner", TenantId = tenant.Id },
                    new Role { Name = "Admin", Description = "Administrator", TenantId = tenant.Id },
                    new Role { Name = "Manager", Description = "Team Lead", TenantId = tenant.Id },
                    new Role { Name = "User", Description = "Employee", TenantId = tenant.Id }
                };

                _context.Roles.AddRange(roles);
                await _context.SaveChangesAsync();

                var superAdminRole = roles.First(r => r.Name == "SuperAdmin");
                
                var user = new User
                {
                    FullName = request.OwnerName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    TenantId = tenant.Id,
                    RoleId = superAdminRole.Id,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GenerateJwtTokenAsync(user, "SuperAdmin", tenant.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users.Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid Credentials");
            }

            return await GenerateJwtTokenAsync(user, user.Role.Name, user.TenantId);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            string accessToken = request.AccessToken;
            string refreshToken = request.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null) throw new Exception("Invalid access token or refresh token");

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired refresh token");
            }

            return await GenerateJwtTokenAsync(user, user.Role.Name, user.TenantId);
        }

        private async Task<AuthResponse> GenerateJwtTokenAsync(User user, string role, Guid tenantId)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("TenantId", tenantId.ToString()) 
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = GenerateRefreshTokenString();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            
            return new AuthResponse(accessToken, refreshToken, role, tenantId);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                ValidateLifetime = false 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}