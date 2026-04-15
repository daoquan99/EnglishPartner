using EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;

namespace EnglishPartner.Application.Tests.Handlers.Learning;

public sealed class ReviewItemCommandHandlerTests
{
    private readonly IItemMasteryRepository          _masteryRepository = Substitute.For<IItemMasteryRepository>();
    private readonly IUserLearningProfileRepository  _profileRepository = Substitute.For<IUserLearningProfileRepository>();
    private readonly ReviewItemCommandHandler        _sut;

    public ReviewItemCommandHandlerTests()
    {
        _sut = new ReviewItemCommandHandler(_masteryRepository, _profileRepository);
    }

    private static ReviewItemCommand ValidCommand(int quality = 4) => new(
        UserId: Guid.NewGuid(),
        ItemId: Guid.NewGuid(),
        Type:   ItemType.Vocabulary,
        QualityRating: quality);

    // ── New mastery (first review) ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenMasteryDoesNotExist_ShouldCreateAndAdd()
    {
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _masteryRepository.Received(1).Add(Arg.Is<ItemMastery>(m =>
            m.UserId == command.UserId && m.ItemId == command.ItemId));
    }

    [Fact]
    public async Task Handle_WhenMasteryDoesNotExist_ShouldNotCallUpdate()
    {
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);

        await _sut.Handle(command, CancellationToken.None);

        _masteryRepository.DidNotReceive().Update(Arg.Any<ItemMastery>());
    }

    // ── Existing mastery (repeat review) ─────────────────────────────────

    [Fact]
    public async Task Handle_WhenMasteryExists_ShouldCallUpdate()
    {
        var command = ValidCommand();
        var mastery = ItemMastery.Create(command.UserId, command.ItemId, command.Type);
        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns(mastery);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _masteryRepository.Received(1).Update(mastery);
    }

    [Fact]
    public async Task Handle_WhenMasteryExists_ShouldNotCallAdd()
    {
        var command = ValidCommand();
        var mastery = ItemMastery.Create(command.UserId, command.ItemId, command.Type);
        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns(mastery);

        await _sut.Handle(command, CancellationToken.None);

        _masteryRepository.DidNotReceive().Add(Arg.Any<ItemMastery>());
    }

    // ── XP grant ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 10)]
    [InlineData(4, 8)]
    [InlineData(3, 6)]
    [InlineData(0, 0)]
    public async Task Handle_ShouldGrantXpEqualToQualityTimes2(int quality, int expectedXp)
    {
        var command = ValidCommand(quality);
        var profile = UserLearningProfile.Create(command.UserId, "Tester");
        var xpBefore = profile.TotalXp;

        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);
        _profileRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        await _sut.Handle(command, CancellationToken.None);

        profile.TotalXp.Should().Be(xpBefore + expectedXp,
            because: $"quality {quality} should grant {expectedXp} XP");
        _profileRepository.Received(1).Update(profile);
    }

    [Fact]
    public async Task Handle_WhenProfileIsNull_ShouldStillReturnSuccess()
    {
        // Profile may not exist yet (e.g. race condition on registration)
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(command.UserId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);
        _profileRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((UserLearningProfile?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _profileRepository.DidNotReceive().Update(Arg.Any<UserLearningProfile>());
    }
}
