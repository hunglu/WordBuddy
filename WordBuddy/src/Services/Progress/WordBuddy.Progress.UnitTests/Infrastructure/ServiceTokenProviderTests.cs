using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WordBuddy.Progress.Infrastructure.Services;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ServiceTokenProviderTests
{
    private const string Secret = "unit-test-secret-do-not-use-in-production-32chars!";
    private const string Issuer = "WordBuddy";

    private readonly ManualTimeProvider _time = new(DateTimeOffset.UtcNow);

    private ServiceTokenProvider CreateProvider() => new(Secret, Issuer, _time);

    [Fact]
    public async Task ServiceTokenProvider_GetToken_HasServiceClaimsIssuerAndFiveMinuteExpiry()
    {
        string token = CreateProvider().GetToken();

        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
        });

        validation.IsValid.Should().BeTrue();
        JsonWebToken jwt = (JsonWebToken)validation.SecurityToken;
        jwt.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        jwt.Issuer.Should().Be(Issuer);
        jwt.Subject.Should().Be("wordbuddy-progress");
        jwt.GetClaim("wb_service").Value.Should().Be("progress");
        (jwt.ValidTo - jwt.IssuedAt).Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ServiceTokenProvider_GetToken_WithinRefreshWindow_ReturnsCachedToken()
    {
        ServiceTokenProvider provider = CreateProvider();
        string first = provider.GetToken();

        _time.Advance(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(59));

        provider.GetToken().Should().Be(first);
    }

    [Fact]
    public void ServiceTokenProvider_GetToken_OneMinuteBeforeExpiry_MintsNewToken()
    {
        ServiceTokenProvider provider = CreateProvider();
        string first = provider.GetToken();

        _time.Advance(TimeSpan.FromMinutes(4));

        provider.GetToken().Should().NotBe(first);
    }

    [Fact]
    public void ServiceTokenProvider_Constructor_EmptySecret_Throws()
    {
        Action act = () => _ = new ServiceTokenProvider(string.Empty, Issuer, _time);

        act.Should().Throw<ArgumentException>();
    }
}
