namespace EnglishLiveTutor.Application.Features.Identity.Contracts;

public interface IUserDirectory
{
    Task<UserLearningProfile?> GetLearningProfileAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<UserNameInfo>> ListUsersByIdOrUsernameAsync(
        IReadOnlyCollection<string> idsOrUsernames,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserNameInfo>> ListUsersByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserNameInfo>> ListUsersByUsernamesAsync(
        IReadOnlyCollection<string> usernames,
        CancellationToken ct = default);
}
