using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services
{
    public interface ITokenService
    {
        public ClaimsPrincipal? ValidateToken(string token);
        public string GenerateAccessToken(string userId, string email, string role);
        public string GenerateRefreshToken();
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<JwtSettings> jwtSettings, ILogger<TokenService> logger, IMemoryCache cache)
        {
            _jwtSettings = jwtSettings.Value;
            _cache = cache;
            _logger = logger;
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var cacheKey = $"token_validation_{token}";

            if (_cache.TryGetValue(cacheKey, out ClaimsPrincipal? cachedPrincipal))
            {
                return cachedPrincipal;
            }

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.AccessTokenSecret));

            var validator = new JwtSecurityTokenHandler();
            try
            {
                var principal = validator.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Не удалось провести валидацию токена");
                return null;
            }
        }

        public string GenerateAccessToken(string userId, string email, string role)
        {
            _logger.LogInformation($"Генерация токена для пользователя {userId}");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.AccessTokenSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            _logger.LogInformation($"Генерация токена для пользователя");
            return new Guid().ToString();
        }
    }
}

