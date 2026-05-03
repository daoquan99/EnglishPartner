using EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.Interfaces;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace EnglishPartner.Application.Tests.Handlers.Learning;

public sealed class ReviewItemCommandHandlerTests
{
    private readonly IItemMasteryRepository          _masteryRepository = Substitute.For<IItemMasteryRepository>();
    private readonly IUserLearningProfileRepository  _profileRepository = Substitute.For<IUserLearningProfileRepository>();
    private readonly TimeProvider                    _timeProvider = Substitute.For<TimeProvider>();
    private readonly ICurrentUserService             _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ReviewItemCommandHandler        _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public ReviewItemCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _timeProvider.GetUtcNow().Returns(_utcNow);
        _sut = new ReviewItemCommandHandler(_masteryRepository, _profileRepository, _timeProvider, _currentUser);
    }

    private static ReviewItemCommand ValidCommand(int quality = 4) => new(
        ItemId: Guid.NewGuid(),
        Type:   ItemType.Vocabulary,
        QualityRating: quality);

    // ── New mastery (first review) ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenMasteryDoesNotExist_ShouldCreateAndAdd()
    {
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _masteryRepository.Received(1).Add(Arg.Is<ItemMastery>(m =>
            m.UserId == _userId && m.ItemId == command.ItemId));
    }

    [Fact]
    public async Task Handle_WhenMasteryDoesNotExist_ShouldNotCallUpdate()
    {
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);

        await _sut.Handle(command, CancellationToken.None);

        _masteryRepository.DidNotReceive().Update(Arg.Any<ItemMastery>());
    }

    // ── Existing mastery (repeat review) ─────────────────────────────────

    [Fact]
    public async Task Handle_WhenMasteryExists_ShouldCallUpdate()
    {
        var command = ValidCommand();
        var mastery = ItemMastery.Create(_userId, command.ItemId, command.Type, _timeProvider.GetUtcNow().UtcDateTime);
        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns(mastery);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _masteryRepository.Received(1).Update(mastery);
    }

    [Fact]
    public async Task Handle_WhenMasteryExists_ShouldNotCallAdd()
    {
        var command = ValidCommand();
        var mastery = ItemMastery.Create(_userId, command.ItemId, command.Type, _timeProvider.GetUtcNow().UtcDateTime);
        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns(mastery);

        await _sut.Handle(command, CancellationToken.None);

        _masteryRepository.DidNotReceive().Add(Arg.Any<ItemMastery>());
    }

    // ── XP grant ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldGrantXpFromPolicy()
    {
        var command = ValidCommand(4);
        var profile = UserLearningProfile.Create(_userId, "tester@example.com", "Tester");
        var xpBefore = profile.TotalXp;

        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);
        _profileRepository
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        await _sut.Handle(command, CancellationToken.None);

        // Standard standalone XP is 2
        profile.TotalXp.Should().Be(xpBefore + 2);
        _profileRepository.Received(1).Update(profile);
    }

    [Fact]
    public async Task Handle_WhenProfileIsNull_ShouldStillReturnSuccess()
    {
        // Profile may not exist yet (e.g. race condition on registration)
        var command = ValidCommand();
        _masteryRepository
            .GetAsync(_userId, command.ItemId, Arg.Any<CancellationToken>())
            .Returns((ItemMastery?)null);
        _profileRepository
            .GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((UserLearningProfile?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _profileRepository.DidNotReceive().Update(Arg.Any<UserLearningProfile>());
    }
}
