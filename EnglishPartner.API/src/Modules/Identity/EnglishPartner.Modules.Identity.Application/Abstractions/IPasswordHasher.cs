namespace EnglishPartner.Modules.Identity.Application.Abstractions;

/// <summary>
/// Abstraction for password hashing — implemented in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
