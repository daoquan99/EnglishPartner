using System.Security.Claims;
using System.Text;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class JwtSecurityTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnglishLiveTutor.Domain.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public void JwtTokenService_WhenSecretKeyLessThan32Bytes_ThrowsException()
    {
        var options = new JwtOptions
        {
            SecretKey = "too_short_key" // < 32 bytes
        };
        var env = new FakeHostEnvironment(Environments.Production);

        var ex = Assert.Throws<InvalidOperationException>(() => new HmacJwtTokenService(options, env));
        Assert.Contains("Jwt:SecretKey", ex.Message);
        Assert.Contains("32 bytes", ex.Message);
    }

    [Fact]
    public void JwtTokenService_NonDevelopmentWithoutKey_ThrowsException()
    {
        var options = new JwtOptions
        {
            SecretKey = null
        };
        var env = new FakeHostEnvironment(Environments.Production);

        var ex = Assert.Throws<InvalidOperationException>(() => new HmacJwtTokenService(options, env));
        Assert.Contains("Jwt:SecretKey", ex.Message);
    }

    [Fact]
    public void JwtTokenService_WithoutEnvironmentAndWithoutKey_ThrowsException()
    {
        var options = new JwtOptions { SecretKey = null };
        var ex = Assert.Throws<InvalidOperationException>(() => new HmacJwtTokenService(options, environment: null));
        Assert.Contains("Jwt:SecretKey", ex.Message);
    }

    [Fact]
    public void JwtTokenService_DefaultConstructorWithoutEnvironmentOrKey_ThrowsException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new HmacJwtTokenService());
        Assert.Contains("Jwt:SecretKey", ex.Message);
    }

    [Fact]
    public void JwtTokenService_DevelopmentWithoutKey_GeneratesValidToken()
    {
        var options = new JwtOptions
        {
            SecretKey = null
        };
        var env = new FakeHostEnvironment(Environments.Development);

        var service = new HmacJwtTokenService(options, env);
        var user = UserEntity.Create("alice", "alice@example.com", "Alice Smith", "hash");

        var tokenResult = service.GenerateAccessToken(user, new[] { "LEARNER" }, new[] { "conversations.access" });

        Assert.NotNull(tokenResult);
        Assert.False(string.IsNullOrWhiteSpace(tokenResult.AccessToken));

        var principal = service.ValidateAccessToken(tokenResult.AccessToken);
        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void JwtTokenService_TokenSignedWithKeyA_CannotBeValidatedWithKeyB()
    {
        var keyA = "ThisIsFirstSecretKeyThatIsAtLeast32BytesLong!";
        var keyB = "ThisIsSecondSecretKeyThatIsAtLeast32BytesLong!";

        var serviceA = new HmacJwtTokenService(new JwtOptions { SecretKey = keyA });
        var serviceB = new HmacJwtTokenService(new JwtOptions { SecretKey = keyB });

        var user = UserEntity.Create("alice", "alice@example.com", "Alice Smith", "hash");
        var tokenFromA = serviceA.GenerateAccessToken(user, new[] { "LEARNER" }, new[] { "conversations.access" });

        var principalValidatedByA = serviceA.ValidateAccessToken(tokenFromA.AccessToken);
        Assert.NotNull(principalValidatedByA);

        var principalValidatedByB = serviceB.ValidateAccessToken(tokenFromA.AccessToken);
        Assert.Null(principalValidatedByB);
    }

    [Fact]
    public void JwtOptionsValidator_RejectsKeyLessThan32Bytes()
    {
        var validator = new JwtOptionsValidator(new FakeHostEnvironment(Environments.Production));
        var result = validator.Validate(null, new JwtOptions { SecretKey = "short_key" });

        Assert.True(result.Failed);
        Assert.Contains("Jwt:SecretKey", result.FailureMessage);
    }

    [Fact]
    public void JwtOptionsValidator_RejectsMissingKeyInNonDevelopment()
    {
        var validator = new JwtOptionsValidator(new FakeHostEnvironment(Environments.Production));
        var result = validator.Validate(null, new JwtOptions { SecretKey = null });

        Assert.True(result.Failed);
        Assert.Contains("Jwt:SecretKey", result.FailureMessage);
    }

    [Fact]
    public void JwtOptionsValidator_AcceptsMissingKeyInDevelopment()
    {
        var validator = new JwtOptionsValidator(new FakeHostEnvironment(Environments.Development));
        var result = validator.Validate(null, new JwtOptions { SecretKey = null });

        Assert.True(result.Succeeded);
    }
}
