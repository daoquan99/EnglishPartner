using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using FluentValidation.TestHelper;

namespace EnglishPartner.Application.Tests.Validators.Identity;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    private static LoginCommand Valid() => new(
        Email: "user@example.com",
        Password: "anypassword");

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
    [InlineData("@domain.com")]
    [InlineData("no-at-sign")]
    public void Should_Fail_WhenEmailFormatIsInvalid(string email)
    {
        _sut.TestValidate(Valid() with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_WhenPasswordIsEmpty()
    {
        _sut.TestValidate(Valid() with { Password = "" })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Pass_WhenPasswordIsAnyNonEmptyString()
    {
        _sut.TestValidate(Valid() with { Password = "x" })
            .ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
