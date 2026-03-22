using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentAssessment.Application.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace StudentAssessment.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public AuthService(IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("Jwt");
        _secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = jwtConfig["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _audience = jwtConfig["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        _expirationMinutes = int.Parse(jwtConfig["ExpirationMinutes"] ?? "60");
    }

    public string GenerateToken(Guid userId, string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BC.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public string HashPassword(string password)
    {
        return BC.HashPassword(password);
    }
}
