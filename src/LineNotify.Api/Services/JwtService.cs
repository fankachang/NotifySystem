using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LineNotify.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LineNotify.Api.Services;

/// <summary>
/// JWT Token 服務實作
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
    }

    /// <inheritdoc/>
    public string GenerateUserToken(int userId, string displayName, bool isAdmin = false, Dictionary<string, string>? claims = null)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("name", displayName),
            new("user_type", "user"),
            new("is_admin", isAdmin.ToString().ToLower())
        };

        // 加入額外的 Claims
        if (claims != null)
        {
            foreach (var claim in claims)
            {
                tokenClaims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        return GenerateToken(tokenClaims);
    }

    /// <inheritdoc/>
    public string GenerateAdminToken(int adminId, string username, bool isSuperAdmin = false)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("name", username),
            new("user_type", "admin"),
            new("is_super_admin", isSuperAdmin.ToString().ToLower())
        };

        return GenerateToken(tokenClaims);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc/>
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, GetValidationParameters(), out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public int? GetUserIdFromToken(string token)
    {
        return GetClaimValue(token, JwtRegisteredClaimNames.Sub, "user");
    }

    /// <inheritdoc/>
    public int? GetAdminIdFromToken(string token)
    {
        return GetClaimValue(token, JwtRegisteredClaimNames.Sub, "admin");
    }

    /// <inheritdoc/>
    public int GetExpiresInSeconds()
    {
        return _jwtSettings.ExpiresInSeconds;
    }

    #region Private Methods

    private string GenerateToken(List<Claim> claims)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddSeconds(_jwtSettings.ExpiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.Zero
        };
    }

    private int? GetClaimValue(string token, string claimType, string expectedUserType)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, GetValidationParameters(), out _);

            var userTypeClaim = principal.FindFirst("user_type");
            if (userTypeClaim?.Value != expectedUserType)
                return null;

            var subClaim = principal.FindFirst(claimType);
            if (subClaim != null && int.TryParse(subClaim.Value, out var id))
            {
                return id;
            }
        }
        catch
        {
            // Token 無效
        }

        return null;
    }

    #endregion
}
