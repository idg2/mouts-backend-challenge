using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Implementation of JWT (JSON Web Token) generator.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the JWT token generator.
    /// </summary>
    /// <param name="configuration">Application configuration containing the necessary keys for token generation.</param>
    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Work item: TASK-048 (FEAT-017), TD-041
    /// <summary>
    /// Generates a JWT token for a specific user.
    /// </summary>
    /// <param name="user">User for whom the token will be generated.</param>
    /// <returns>Valid JWT token as string.</returns>
    /// <remarks>
    /// The generated token includes the following claims:
    /// - NameIdentifier (User ID)
    /// - Name (Username)
    /// - Role (User role)
    /// 
    /// The token is valid for 8 hours from the moment of generation.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when <c>Jwt:SecretKey</c> is not configured.</exception>
    public string GenerateToken(IUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured. Set it in appsettings or via the Jwt__SecretKey environment variable.");
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new[]
        {
           new Claim(ClaimTypes.NameIdentifier, user.Id),
           new Claim(ClaimTypes.Name, user.Username),
           new Claim(ClaimTypes.Role, user.Role)
       };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        StepTrace.Step("AUT-LGN-07", "Generate the JWT", [("userId", user.Id), ("expires", tokenDescriptor.Expires)]);

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}