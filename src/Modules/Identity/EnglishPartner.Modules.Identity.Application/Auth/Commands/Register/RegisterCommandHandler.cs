using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository,
    IStreakRepository streakRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            return Result.Failure<RegisterResponse>(IdentityErrors.User.EmailAlreadyExists);

        var passwordHash = passwordHasher.Hash(request.Password);
        var user         = User.Create(request.Email, passwordHash, request.DisplayName);
        var profile      = UserProfile.Create(user.Id);
        var streak       = Streak.Create(user.Id);

        // Assign Learner role by default
        var learnerRole = await roleRepository.GetByNameAsync("Learner", cancellationToken);
        if (learnerRole is not null)
            user.AssignRole(learnerRole.Id);

        userRepository.Add(user);
        profileRepository.Add(profile);
        streakRepository.Add(streak);

        // Learner has no permission claims — just the role name is enough for user endpoints
        var roleNames   = learnerRole is not null ? new[] { learnerRole.Name } : Array.Empty<string>();
        var permissions = Array.Empty<string>(); // Learner endpoints use RequireAuthorization() only
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions);

        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        refreshTokenRepository.Add(refreshToken);

        return new RegisterResponse(user.Id, accessToken, refreshToken.Token);
    }
}
