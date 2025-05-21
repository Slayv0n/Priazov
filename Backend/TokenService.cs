using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using DataBase.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend
{
    public class TokenService
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

        // Валидация токена (для Access и Refresh)
        public ClaimsPrincipal? ValidateToken(string token, bool isAccessToken)
        {
            var cacheKey = $"token_validation_{token}";

            if (_cache.TryGetValue(cacheKey, out ClaimsPrincipal? cachedPrincipal))
            {
                return cachedPrincipal;
            }

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                isAccessToken ? _jwtSettings.AccessTokenSecret : _jwtSettings.RefreshTokenSecret));

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

                _cache.Set(cacheKey, principal, TimeSpan.FromMinutes(1));

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
                new Claim(ClaimTypes.Role, role),
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

        // Генерация Refresh Token (обычно случайная строка, но может быть и JWT)
        public string GenerateRefreshToken(string userId)
        {
            _logger.LogInformation($"Генерация токена для пользователя {userId}");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.RefreshTokenSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_jwtSettings.RefreshTokenExpiryDays)),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}

