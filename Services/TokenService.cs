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
        // Get the security key value from appsetting.json.
        var secretKey = _configuration["JwtSettings:SecretKey"];
        // Convert security key to a byte array, then create SymmetricSecurityKey.
        // This key will be used for both signing and verification of token.  
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        // Generate new SigningCredentials with key and algorithm. 
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create a new claim for user when logged in with their data. 
        var claims = new[]
        {
            // User Id
            new Claim("userId", user.UserId.ToString()),
            // Username
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            // Unique ID for token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Generate a token with metadata. claims, expire time, and credentials. 
        var token = new JwtSecurityToken(
            issuer: "NoteTakingApp",
            audience: "NoteTakingApp",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        // Return the generated token as a string. 
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}