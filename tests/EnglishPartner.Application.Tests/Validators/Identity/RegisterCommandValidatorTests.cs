using EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;
using FluentValidation.TestHelper;

namespace EnglishPartner.Application.Tests.Validators.Identity;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    private static RegisterCommand Valid() => new(
        Email: "valid@example.com",
        Password: "SecurePass1!",
        DisplayName: "Nguyen Van A");

    // ── Email ─────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenEmailIsEmpty()
    {
        _sut.TestValidate(Valid() with { Email = "" })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("plain text")]
    public void Should_Fail_WhenEmailFormatIsInvalid(string invalidEmail)
    {
        _sut.TestValidate(Valid() with { Email = invalidEmail })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_WhenEmailExceeds256Chars()
    {
        var longEmail = new string('a', 251) + "@b.com"; // 257 chars > 256 limit
        _sut.TestValidate(Valid() with { Email = longEmail })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ── Password ──────────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_WhenPasswordIsEmpty()
    {
        _sut.TestValidate(Valid() with { Password = "" })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("abc")]     // 3 chars — too short
    [InlineData("1234567")] // 7 chars — too short
    public void Should_Fail_WhenPasswordTooShort(string pwd)
    {
        _sut.TestValidate(Valid() with { Password = pwd })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_WhenPasswordExceeds128Chars()
    {
        _sut.TestValidate(Valid() with { Password = new string('a', 129) })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Pass_WhenPasswordIsExactly8Chars()
    {
        _sut.TestValidate(Valid() with { Password = "12345678" })
            .ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // ── DisplayName ───────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_WhenDisplayNameIsEmpty()
    {
        _sut.TestValidate(Valid() with { DisplayName = "" })
            .ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Should_Fail_WhenDisplayNameExceeds100Chars()
    {
        _sut.TestValidate(Valid() with { DisplayName = new string('A', 101) })
            .ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Should_Pass_WhenDisplayNameIsExactly100Chars()
    {
        _sut.TestValidate(Valid() with { DisplayName = new string('A', 100) })
            .ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }
}
