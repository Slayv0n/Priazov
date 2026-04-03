using Backend.Models;
using Backend.Models.Dto;
using Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Backend.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private ILogger<TokenRefreshMiddleware> _logger;

        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService, IOptions<JwtSettings> jwtSettings)
        {
            _logger.LogInformation("Middleware work");
            var accessToken = context.Request.Cookies["access_token"]
                   ?? context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

                    if (jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(1))
                    {
                        _logger.LogInformation("Access token needs refresh");
                        await RefreshToken(context, refreshToken, authService, jwtSettings);
                    }
                }
                else
                {
                    await RefreshToken(context, refreshToken, authService, jwtSettings);
                }
                
            }
            
            await _next(context);
        }
        private async Task RefreshToken(HttpContext context, string refreshToken, IAuthService authService, IOptions<JwtSettings> jwtSettings)
        {
            var newTokens = await authService.Refresh(new RefreshDto(refreshToken));

            if (newTokens != null)
            {
                _logger.LogInformation("Tokens refreshed successfully");

                var claimsPrincipal = authService.CreateClaimsPrincipalFromToken(newTokens.AccessToken);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes),
                        IsPersistent = true,
                        AllowRefresh = true
                    });

                context.Response.Cookies.Append("access_token", newTokens.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes)
                });

                context.Response.Cookies.Append("refresh_token", newTokens.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays)
                });

                context.User = claimsPrincipal;
            }
        }
    }
}
