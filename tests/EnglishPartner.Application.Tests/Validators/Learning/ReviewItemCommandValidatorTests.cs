using EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using FluentValidation.TestHelper;

namespace EnglishPartner.Application.Tests.Validators.Learning;

public sealed class ReviewItemCommandValidatorTests
{
    private readonly ReviewItemCommandValidator _sut = new();

    private static ReviewItemCommand Valid() => new(
        UserId: Guid.NewGuid(),
        ItemId: Guid.NewGuid(),
        Type: ItemType.Vocabulary,
        QualityRating: 4);

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    // ── QualityRating boundaries ──────────────────────────────────────────

    [Theory]
    [InlineData(0)] // Blackout — valid
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)] // Perfect — valid
    public void Should_Pass_WhenQualityRatingIsInValidRange(int quality)
    {
        _sut.TestValidate(Valid() with { QualityRating = quality })
            .ShouldNotHaveValidationErrorFor(x => x.QualityRating);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(-100)]
    [InlineData(100)]
    public void Should_Fail_WhenQualityRatingIsOutOfRange(int quality)
    {
        _sut.TestValidate(Valid() with { QualityRating = quality })
            .ShouldHaveValidationErrorFor(x => x.QualityRating);
    }

    // ── Required GUIDs ────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_WhenUserIdIsEmpty()
    {
        _sut.TestValidate(Valid() with { UserId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Fail_WhenItemIdIsEmpty()
    {
        _sut.TestValidate(Valid() with { ItemId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ItemId);
    }
}
