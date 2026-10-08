using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WordBuddy.Progress.Api.Authorization;
using WordBuddy.Progress.Application.Extensions;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Extensions;

namespace WordBuddy.Progress.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT bearer validation using the same signing key/issuer Identity uses to issue
    /// tokens — this service never issues tokens itself, only validates them.
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

        services.AddAuthorization(options => options.AddSupportLinkPolicies());
        services.AddSupportLinkAuthorizationHandlers();

        return services;
    }

    /// <summary>Registers the <see cref="ProgressDbContext"/> and Infrastructure-layer services.</summary>
    public static IServiceCollection AddWordBuddyDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        return services;
    }

    /// <summary>
    /// Binds the vocabulary SRS options (<c>Vocabulary:Scheduling</c>, <c>Vocabulary:Grading</c>,
    /// <c>Vocabulary:NewWordCap</c>) and registers the pure domain services built from them.
    /// </summary>
    public static IServiceCollection AddVocabularySrs(this IServiceCollection services, IConfiguration configuration)
    {
        VocabularySchedulingOptions scheduling =
            configuration.GetSection(VocabularySchedulingOptions.SectionName).Get<VocabularySchedulingOptions>() ?? new();
        VocabularyGradingOptions grading =
            configuration.GetSection(VocabularyGradingOptions.SectionName).Get<VocabularyGradingOptions>() ?? new();
        NewWordCapOptions newWordCap =
            configuration.GetSection(NewWordCapOptions.SectionName).Get<NewWordCapOptions>() ?? new();

        services.AddSingleton(scheduling);
        services.AddSingleton(grading);
        services.AddSingleton(newWordCap);
        services.AddSingleton<IFsrsScheduler>(new FsrsScheduler(scheduling));
        services.AddSingleton(new AnswerGrader(grading));
        services.AddSingleton(new NewWordCapPolicy(newWordCap));

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
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "WordBuddy Progress API", Version = "v1" });

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
