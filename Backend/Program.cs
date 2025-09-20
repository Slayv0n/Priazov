using Backend.Mapping;
using Backend.Middleware;
using Backend.Models;
using Backend.Services;
using DataBase;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.LowercaseQueryStrings = true;
    o.AppendTrailingSlash = true;
        
});

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SMTP"));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<DadataSettings>(builder.Configuration.GetSection("Dadata"));

builder.Services.Configure<TurnstileSettings>(builder.Configuration.GetSection("Turnstile"));

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();

builder.Services.AddHostedService<SessionsCleanupService>();
builder.Services.AddHostedService<PasswordTokensCleanupService>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IMessageService, EmailService>();

builder.Services.AddScoped<IManagerService, ManagerService>();

builder.Services.AddScoped<ICompanyService, CompanyService>();

builder.Services.AddScoped<ICommentService, CommentService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IPasswordService, PasswordService>();    

builder.Services.AddDbContextFactory<PriazovContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Singleton);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    var settings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

    options.LoginPath = "/auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(settings.RefreshTokenExpiryDays);
    options.SlidingExpiration = true;
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<PriazovContext>();

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "API V1", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHttpsRedirection();
}
else
{
    app.UseSwagger(opt =>
    {
        opt.RouteTemplate = "openapi/{documentName}.json";
    });
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<TokenRefreshMiddleware>();
app.UseAuthorization();

app.MapScalarApiReference(opt =>
{
    opt.Title = "Scalar Documentation";
    opt.Theme = ScalarTheme.BluePlanet;
    opt.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
});

app.MapCompanyEndpoints();
app.MapManagerEndpoints();

app.Run();

