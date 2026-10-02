using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WordBuddy.Content.Application.Extensions;
using WordBuddy.Content.Infrastructure.Extensions;

namespace WordBuddy.Content.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT bearer validation using the same signing key/issuer Identity uses to issue
    /// tokens (<c>Jwt:Secret</c>/<c>Jwt:Issuer</c> in config — this service never issues tokens
    /// itself, only validates them).
    /// </summary>
    public static IServiceCollection AddWordBuddyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection jwtSection = configuration.GetSection("Jwt");
        string secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("'Jwt:Secret' configuration is required.");
        string issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("'Jwt:Issuer' configuration is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireClaim("is_admin", "true"));

            // Child accounts cannot initiate sharing — this feature's first-principles
            // child-safety gate on the write side. An admin may act on any word regardless of
            // their own age group.
            options.AddPolicy("CanShareVocabulary", policy => policy.RequireAssertion(context =>
                string.Equals(context.User.FindFirst("is_admin")?.Value, "true", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(context.User.FindFirst("age_group")?.Value, "Child", StringComparison.OrdinalIgnoreCase)));
        });

        return services;
    }

    /// <summary>Registers the <see cref="ContentDbContext"/> and Infrastructure-layer services.</summary>
    public static IServiceCollection AddWordBuddyDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers Application-layer handlers, controllers, and Swagger.</summary>
    public static IServiceCollection AddWordBuddyServices(this IServiceCollection services)
    {
        services.AddApplication();

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "WordBuddy Content API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return services;
    }
}
