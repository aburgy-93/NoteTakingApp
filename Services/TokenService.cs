using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;


namespace Backend.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // User Id
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            // Username
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            // Unique ID for token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "NoteTakingApp",
            audience: "NoteTakingApp",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}