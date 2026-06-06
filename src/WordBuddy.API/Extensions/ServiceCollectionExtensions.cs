using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WordBuddy.API.Services;
using WordBuddy.API.Settings;
using WordBuddy.Application.Extensions;
using WordBuddy.Infrastructure.Extensions;

namespace WordBuddy.API.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication, authorization, and the <see cref="JwtTokenGenerator"/>.
    /// Reads <c>Jwt:Secret</c>, <c>Jwt:Issuer</c>, and <c>Jwt:ExpiryMinutes</c> from configuration.
    /// </summary>
    internal static IServiceCollection AddWordBuddyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtSettings jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("'Jwt' configuration section is required.");

        services.AddSingleton(jwtSettings);
        services.AddScoped<JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = false,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew                = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Registers EF Core, all repositories, and infrastructure services.
    /// Reads <c>ConnectionStrings:DefaultConnection</c> from configuration.
    /// </summary>
    internal static IServiceCollection AddWordBuddyDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        return services;
    }

    /// <summary>
    /// Registers CORS, MVC controllers, Swagger, and all application-layer handlers and validators.
    /// </summary>
    internal static IServiceCollection AddWordBuddyServices(this IServiceCollection services)
    {
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "WordBuddy API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter your JWT token.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });

        services.AddApplication();
        return services;
    }
}
