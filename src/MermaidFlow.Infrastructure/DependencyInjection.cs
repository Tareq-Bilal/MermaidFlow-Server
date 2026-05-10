using MermaidFlow.Application.Common.Interfaces;
using MermaidFlow.Infrastructure.Auth;
using MermaidFlow.Infrastructure.Common.Persistence;
using MermaidFlow.Infrastructure.Common.Security;
using MermaidFlow.Infrastructure.Documents;
using MermaidFlow.Infrastructure.Mermaid;
using MermaidFlow.Infrastructure.Persistence;
using MermaidFlow.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MermaidFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MermaidFlowDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDiagramCacheRepository, DiagramCacheRepository>();
        services.AddScoped<IThemeRepository, ThemeRepository>();
        services.AddScoped<IDocumentExporter, MarkdigDocumentExporter>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddMermaidRenderer(configuration);

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

    private static IServiceCollection AddMermaidRenderer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MermaidRendererOptions>(
            configuration.GetSection(MermaidRendererOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<MermaidRendererOptions>>().Value;

            return PlaywrightPagePool.CreateAsync(options).GetAwaiter().GetResult();
        });

        services.AddScoped<IMermaidRenderer, PlaywrightMermaidRenderer>();

        return services;
    }
}
