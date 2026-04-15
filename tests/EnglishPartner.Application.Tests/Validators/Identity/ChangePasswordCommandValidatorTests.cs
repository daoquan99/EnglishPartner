using EnglishPartner.Modules.Identity.Application.Auth.Commands.ChangePassword;
using FluentValidation.TestHelper;

namespace EnglishPartner.Application.Tests.Validators.Identity;

public sealed class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _sut = new();

    private static ChangePasswordCommand Valid() => new(
        UserId: Guid.NewGuid(),
        CurrentPassword: "OldPassword1!",
        NewPassword: "NewPassword2!");

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenCurrentPasswordIsEmpty()
    {
        _sut.TestValidate(Valid() with { CurrentPassword = "" })
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void Should_Fail_WhenNewPasswordIsEmpty()
    {
        _sut.TestValidate(Valid() with { NewPassword = "" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    // Note: UserId is extracted from JWT claims — not part of this validator
}
