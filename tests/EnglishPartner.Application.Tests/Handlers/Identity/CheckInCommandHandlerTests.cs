using EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;

namespace EnglishPartner.Application.Tests.Handlers.Identity;

public sealed class CheckInCommandHandlerTests
{
    private readonly IStreakRepository _streakRepository = Substitute.For<IStreakRepository>();
    private readonly CheckInCommandHandler _sut;

    public CheckInCommandHandlerTests()
    {
        _sut = new CheckInCommandHandler(_streakRepository);
    }

    // ── Streak not found ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStreakNotFound_ShouldReturnNotFoundFailure()
    {
        var command = new CheckInCommand(Guid.NewGuid());
        _streakRepository.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
                         .Returns((Streak?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IdentityErrors.Streak.NotFound);
    }

    // ── Already checked in today ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAlreadyCheckedIn_ShouldReturnAlreadyCheckedInError()
    {
        var userId  = Guid.NewGuid();
        var command = new CheckInCommand(userId);
        var streak  = Streak.Create(userId);
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        streak.CheckIn(today); // already checked in

        _streakRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                         .Returns(streak);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IdentityErrors.Streak.AlreadyCheckedIn);
    }

    // ── Success ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStreakExists_ShouldReturnSuccessWithUpdatedStreak()
    {
        var userId  = Guid.NewGuid();
        var command = new CheckInCommand(userId);
        var streak  = Streak.Create(userId); // no previous activity

        _streakRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                         .Returns(streak);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStreak.Should().Be(1);
        result.Value!.LongestStreak.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Success_ShouldCallRepositoryUpdate()
    {
        var userId  = Guid.NewGuid();
        var command = new CheckInCommand(userId);
        var streak  = Streak.Create(userId);

        _streakRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                         .Returns(streak);

        await _sut.Handle(command, CancellationToken.None);

        _streakRepository.Received(1).Update(streak);
    }

    [Fact]
    public async Task Handle_Success_ShouldReturnCorrectLastActivityDate()
    {
        var userId  = Guid.NewGuid();
        var command = new CheckInCommand(userId);
        var streak  = Streak.Create(userId);
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);

        _streakRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                         .Returns(streak);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Value!.LastActivityDate.Should().Be(today);
    }
}
