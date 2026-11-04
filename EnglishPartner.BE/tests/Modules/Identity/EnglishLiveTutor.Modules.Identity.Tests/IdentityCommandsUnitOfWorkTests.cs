using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class IdentityCommandsUnitOfWorkTests
{
    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        public int CommitCount { get; private set; }

        public CountingUnitOfWork(IUnitOfWork inner)
        {
            _inner = inner;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            CommitCount++;
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken ct = default)
        {
            return _inner.ExecuteInTransactionAsync(operation, ct);
        }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; } = Guid.NewGuid().ToString();
        public string? Email { get; set; } = "tester@example.com";
        public string? FullName { get; set; } = "Tester";
        public string UserIdOrSystem => UserId ?? "SYSTEM";
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public bool HasRole(string role) => true;
        public bool HasPermission(string permission) => true;
    }

    #region Reflection Test: Không handler nào còn phụ thuộc IAppDbContextFactory

    [Fact]
    public void NoCommandHandlersInIdentityCommandsNamespace_ShouldDependOn_IAppDbContextFactory()
    {
        var assembly = typeof(AdminCreateUserCommandHandler).Assembly;
        var commandHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.Namespace != null &&
                        t.Namespace.StartsWith("EnglishLiveTutor.Application.Features.Identity.Commands") &&
                        t.Name.EndsWith("CommandHandler"))
            .ToList();

        Assert.NotEmpty(commandHandlerTypes);
        Assert.True(commandHandlerTypes.Count >= 14, $"Expected at least 14 handlers, found {commandHandlerTypes.Count}");

        foreach (var handlerType in commandHandlerTypes)
        {
            var ctors = handlerType.GetConstructors();
            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                var hasDbFactory = parameters.Any(p => p.ParameterType == typeof(IAppDbContextFactory));
                Assert.False(hasDbFactory, $"{handlerType.Name} constructor still depends on IAppDbContextFactory!");
            }
        }
    }

    #endregion

    #region CountingUnitOfWork Commit Count Tests (Login, RegisterUser, RefreshToken, AdminCreateUser, UpdateRolePermissions, CreatePermission, RevokeRefreshToken -> đúng 1 lần)

    [Fact]
    public async Task AdminCreateUser_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var hasher = new Pbkdf2PasswordHasher();

        var learnerRole = new RoleEntity("LEARNER", "Learner", "Learner role", isSystemRole: true);
        db.Roles.Add(learnerRole);
        await db.SaveChangesAsync();

        var handler = new AdminCreateUserCommandHandler(db, countingUow, hasher);
        var cmd = new AdminCreateUserCommand(new AdminCreateUserRequest(
            Username: "adminuser",
            Email: "adminuser@example.com",
            Password: "Password#123",
            FullName: "Admin Created User",
            RoleCodes: new List<string> { "LEARNER" }));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.Equal("adminuser", result.Username);
        Assert.Equal(1, countingUow.CommitCount);

        // Verify state persisted
        await using var verifyDb = new AppDbContext(options);
        var persisted = await verifyDb.Users.FirstOrDefaultAsync(u => u.Username == "adminuser");
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task UpdateRolePermissions_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var roleRepo = new RoleRepository(db);

        var role = new RoleEntity("MODERATOR", "Moderator", "Moderator role", isSystemRole: false);
        var perm1 = new PermissionEntity("mod.read", "Read Mod", "Mod", "Read");
        var perm2 = new PermissionEntity("mod.write", "Write Mod", "Mod", "Write");
        db.Roles.Add(role);
        db.Permissions.AddRange(perm1, perm2);
        await db.SaveChangesAsync();

        var handler = new UpdateRolePermissionsCommandHandler(roleRepo, db, countingUow);
        var cmd = new UpdateRolePermissionsCommand(role.Id, new UpdateRolePermissionsRequest(
            Name: "Moderator Updated",
            Description: "Updated desc",
            PermissionCodes: new List<string> { "mod.read", "mod.write" }));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.Equal(2, result.Permissions.Count);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var persistedRole = await verifyDb.Roles.Include(r => r.RolePermissions).FirstAsync(r => r.Id == role.Id);
        Assert.Equal("Moderator Updated", persistedRole.Name);
        Assert.Equal(2, persistedRole.RolePermissions.Count);
    }

    [Fact]
    public async Task Login_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var hasher = new Pbkdf2PasswordHasher();
        var userRepo = new UserRepository(db);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });

        var user = UserEntity.Create("logintest", "logintest@example.com", "Login Test", hasher.HashPassword("Password#123"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(userRepo, db, countingUow, hasher, jwtService);
        var cmd = new LoginCommand(new LoginRequest("logintest", "Password#123"), "127.0.0.1");

        var response = await handler.HandleAsync(cmd);

        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(1, countingUow.CommitCount);
    }

    [Fact]
    public async Task RegisterUser_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var hasher = new Pbkdf2PasswordHasher();
        var userRepo = new UserRepository(db);
        var roleRepo = new RoleRepository(db);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });

        var learnerRole = new RoleEntity("LEARNER", "Learner", "Learner role", isSystemRole: true);
        db.Roles.Add(learnerRole);
        await db.SaveChangesAsync();

        var handler = new RegisterUserCommandHandler(userRepo, roleRepo, db, countingUow, hasher, jwtService);
        var cmd = new RegisterUserCommand(new RegisterUserRequest(
            Username: "newregistrant",
            Email: "newregistrant@example.com",
            Password: "Password#123",
            FullName: "New Registrant"), "127.0.0.1");

        var response = await handler.HandleAsync(cmd);

        Assert.NotNull(response);
        Assert.NotEmpty(response.AccessToken);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var persisted = await verifyDb.Users.FirstOrDefaultAsync(u => u.Username == "newregistrant");
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task RefreshToken_Rotation_CommitsExactlyOnceViaUoW()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var userRepo = new UserRepository(db);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var user = UserEntity.Create("rotatetest", "rotate@example.com", "Rotate Test", "hash");
        string rawToken1 = jwtService.GenerateRefreshTokenString();
        string tokenHash1 = RefreshTokenEntity.HashToken(rawToken1);
        user.IssueRefreshToken(tokenHash1, DateTimeOffset.UtcNow.AddDays(30), "127.0.0.1", "device");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(userRepo, db, countingUow, jwtService, timeProvider: clock);
        var cmd = new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1");

        var response = await handler.HandleAsync(cmd);

        Assert.NotNull(response);
        Assert.NotEmpty(response.RefreshToken);
        Assert.NotEqual(rawToken1, response.RefreshToken);
        Assert.Equal(1, countingUow.CommitCount);
    }

    [Fact]
    public async Task CreatePermission_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var permRepo = new PermissionRepository(db);

        var handler = new CreatePermissionCommandHandler(permRepo, countingUow);
        var cmd = new CreatePermissionCommand(new CreatePermissionRequest(
            Code: "test.perm.create",
            Name: "Test Perm",
            Module: "Testing",
            Description: "Test Description"));

        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.Equal("test.perm.create", result.Code);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var permInDb = await verifyDb.Permissions.FirstOrDefaultAsync(p => p.Code == "test.perm.create");
        Assert.NotNull(permInDb);
    }

    [Fact]
    public async Task RevokeRefreshToken_CommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var userRepo = new UserRepository(db);

        var user = UserEntity.Create("revoker", "revoker@example.com", "Revoker", "hash");
        string rawToken = "raw-test-token-to-revoke";
        string tokenHash = RefreshTokenEntity.HashToken(rawToken);
        user.IssueRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(7), "127.0.0.1", "device");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new RevokeRefreshTokenCommandHandler(userRepo, countingUow);
        var cmd = new RevokeRefreshTokenCommand(new RevokeTokenRequest(rawToken, "User logout"));

        var result = await handler.HandleAsync(cmd);

        Assert.True(result);
        Assert.Equal(1, countingUow.CommitCount);

        await using var verifyDb = new AppDbContext(options);
        var tokenInDb = await verifyDb.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash);
        Assert.NotNull(tokenInDb);
        Assert.True(tokenInDb.IsRevoked);
        Assert.Equal("User logout", tokenInDb.RevokedReason);
    }

    #endregion

    #region All Other Handlers Execute Successfully with InMemory IAppDbContext + IUnitOfWork

    [Fact]
    public async Task AdminUpdateUser_UpdatesUserSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var hasher = new Pbkdf2PasswordHasher();

        var user = UserEntity.Create("updatetest", "update@example.com", "Old Name", hasher.HashPassword("OldPass#123"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new AdminUpdateUserCommandHandler(db, uow, hasher);
        var result = await handler.HandleAsync(new AdminUpdateUserCommand(user.Id, new AdminUpdateUserRequest(
            Email: "update@example.com",
            FullName: "New Name",
            IsActive: true,
            NewPassword: "NewPass#123")));

        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);

        await using var verifyDb = new AppDbContext(options);
        var persisted = await verifyDb.Users.FirstAsync(u => u.Id == user.Id);
        Assert.Equal("New Name", persisted.FullName);
        Assert.True(hasher.VerifyPassword("NewPass#123", persisted.PasswordHash));
    }

    [Fact]
    public async Task AssignUserRoles_AssignsRolesSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var userRepo = new UserRepository(db);

        var user = UserEntity.Create("assignroleuser", "assign@example.com", "Assign User", "hash");
        var role1 = new RoleEntity("ROLE_A", "Role A", "A", isSystemRole: false);
        var role2 = new RoleEntity("ROLE_B", "Role B", "B", isSystemRole: false);
        db.Users.Add(user);
        db.Roles.AddRange(role1, role2);
        await db.SaveChangesAsync();

        var handler = new AssignUserRolesCommandHandler(userRepo, db, uow);
        var result = await handler.HandleAsync(new AssignUserRolesCommand(user.Id, new AssignUserRolesRequest(new List<string> { "ROLE_A", "ROLE_B" })));

        Assert.NotNull(result);
        Assert.Contains("ROLE_A", result.Roles);
        Assert.Contains("ROLE_B", result.Roles);

        await using var verifyDb = new AppDbContext(options);
        var userInDb = await verifyDb.Users.Include(u => u.UserRoles).FirstAsync(u => u.Id == user.Id);
        Assert.Equal(2, userInDb.UserRoles.Count);
    }

    [Fact]
    public async Task CreateRole_CreatesRoleSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var roleRepo = new RoleRepository(db);

        var perm = new PermissionEntity("perm.one", "Perm One", "Module", "Desc");
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();

        var handler = new CreateRoleCommandHandler(roleRepo, db, uow);
        var result = await handler.HandleAsync(new CreateRoleCommand(new CreateRoleRequest(
            Code: "NEW_ROLE",
            Name: "New Role Name",
            Description: "Role Desc",
            PermissionCodes: new List<string> { "perm.one" })));

        Assert.NotNull(result);
        Assert.Equal("NEW_ROLE", result.Code);
        Assert.Contains("perm.one", result.Permissions);

        await using var verifyDb = new AppDbContext(options);
        var roleInDb = await verifyDb.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Code == "NEW_ROLE");
        Assert.NotNull(roleInDb);
        Assert.Single(roleInDb!.RolePermissions);
    }

    [Fact]
    public async Task DeletePermission_SoftDeletesSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser();

        var perm = new PermissionEntity("del.perm", "Del Perm", "Module", "Desc");
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();

        var handler = new DeletePermissionCommandHandler(db, uow, currentUser);
        bool deleted = await handler.HandleAsync(new DeletePermissionCommand(perm.Id));

        Assert.True(deleted);

        await using var verifyDb = new AppDbContext(options);
        var permInDb = await verifyDb.Permissions.IgnoreQueryFilters().FirstAsync(p => p.Id == perm.Id);
        Assert.True(permInDb.IsDeleted);
        Assert.False(await verifyDb.Permissions.AnyAsync(p => p.Id == perm.Id));
    }

    [Fact]
    public async Task DeleteRole_SoftDeletesNonSystemRole_ThrowsForSystemRole()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser();

        var customRole = new RoleEntity("CUSTOM_ROLE", "Custom", "Desc", isSystemRole: false);
        var sysRole = new RoleEntity("SYS_ROLE", "System", "Desc", isSystemRole: true);
        db.Roles.AddRange(customRole, sysRole);
        await db.SaveChangesAsync();

        var handler = new DeleteRoleCommandHandler(db, uow, currentUser);

        // System role throws
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new DeleteRoleCommand(sysRole.Id)));

        // Non-system role soft deletes
        bool deleted = await handler.HandleAsync(new DeleteRoleCommand(customRole.Id));
        Assert.True(deleted);

        await using var verifyDb = new AppDbContext(options);
        var roleInDb = await verifyDb.Roles.IgnoreQueryFilters().FirstAsync(r => r.Id == customRole.Id);
        Assert.True(roleInDb.IsDeleted);
        Assert.False(await verifyDb.Roles.AnyAsync(r => r.Id == customRole.Id));
    }

    [Fact]
    public async Task DeleteUser_SoftDeletesUserSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser();

        var user = UserEntity.Create("usertodelete", "del@example.com", "Del User", "hash");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new DeleteUserCommandHandler(db, uow, currentUser);
        bool deleted = await handler.HandleAsync(new DeleteUserCommand(user.Id));

        Assert.True(deleted);

        await using var verifyDb = new AppDbContext(options);
        var userInDb = await verifyDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == user.Id);
        Assert.True(userInDb.IsDeleted);
        Assert.False(await verifyDb.Users.AnyAsync(u => u.Id == user.Id));
    }

    [Fact]
    public async Task UpdatePermission_UpdatesDetailsSuccessfully()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        var perm = new PermissionEntity("perm.edit", "Old Name", "OldMod", "OldDesc");
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();

        var handler = new UpdatePermissionCommandHandler(db, uow);
        var result = await handler.HandleAsync(new UpdatePermissionCommand(perm.Id, new AdminUpdatePermissionRequest(
            Name: "New Name",
            Module: "NewMod",
            Description: "NewDesc")));

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("NewMod", result.Module);

        await using var verifyDb = new AppDbContext(options);
        var permInDb = await verifyDb.Permissions.FirstAsync(p => p.Id == perm.Id);
        Assert.Equal("New Name", permInDb.Name);
    }

    #endregion

    #region RefreshToken Reuse: token đã xoay bị dùng lại -> 401 và toàn bộ chuỗi token bị thu hồi trong DB

    [Fact]
    public async Task RefreshToken_ReuseAfterGracePeriod_RevokesAllActiveTokensInDatabase()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var userRepo = new UserRepository(db);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var t0 = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(t0);

        var user = UserEntity.Create("reusevictim", "victim@example.com", "Victim", "hash");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Cấp token 1
        var loginHandler = new LoginCommandHandler(userRepo, db, uow, new Pbkdf2PasswordHasher(), jwtService);
        string rawPassword = "VictimPass#123";
        user.ChangePassword(new Pbkdf2PasswordHasher().HashPassword(rawPassword));
        await db.SaveChangesAsync();

        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("reusevictim", rawPassword), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        // Xoay token 1 -> nhận token 2
        var refreshOptions = new RefreshTokenOptions(10);
        var refreshHandler = new RefreshTokenCommandHandler(userRepo, db, uow, jwtService, refreshOptions, clock);
        var rotateRes = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1"));
        string rawToken2 = rotateRes.RefreshToken;

        // Kiểm tra token 2 đang active
        string token2Hash = RefreshTokenEntity.HashToken(rawToken2);
        await using (var checkDb = new AppDbContext(options))
        {
            var t2 = await checkDb.RefreshTokens.FirstAsync(rt => rt.Token == token2Hash);
            Assert.False(t2.IsRevoked);
        }

        // Tái sử dụng token 1 ngoài khoảng ân hạn (15s > 10s grace)
        clock.Advance(TimeSpan.FromSeconds(15));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex.Message);

        // Đọc bằng DbContext MỚI: TOÀN BỘ chuỗi token của user (kể cả token 2) phải bị thu hồi!
        await using (var newDb = new AppDbContext(options))
        {
            var userTokens = await newDb.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
            Assert.NotEmpty(userTokens);
            Assert.All(userTokens, rt =>
            {
                Assert.True(rt.IsRevoked, $"Token {rt.Id} should be revoked");
            });

            var t2InDb = userTokens.First(rt => rt.Token == token2Hash);
            Assert.True(t2InDb.IsRevoked);
            Assert.Equal("Reuse detected", t2InDb.RevokedReason);
        }
    }

    #endregion

    #region Login: token hết hạn/thu hồi bị purge & JWT chứa đúng role/permission sau khi AssignUserRoles

    [Fact]
    public async Task Login_PurgesExpiredAndLongRevokedTokens_InSingleCommit()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var userRepo = new UserRepository(db);
        var hasher = new Pbkdf2PasswordHasher();
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var now = DateTimeOffset.UtcNow;

        var user = UserEntity.Create("purgetest", "purge@example.com", "Purge Test", hasher.HashPassword("Pass#123456"));
        db.Users.Add(user);

        // 1 token hết hạn (expired 5 days ago)
        var expiredToken = new RefreshTokenEntity(user.Id, "expired-token-hash", now.AddDays(-5), "127.0.0.1", "device");
        // 1 token đã thu hồi 35 ngày trước (> 30 days purge window)
        var oldRevokedToken = new RefreshTokenEntity(user.Id, "old-revoked-token-hash", now.AddDays(10), "127.0.0.1", "device");
        oldRevokedToken.Revoke(now.AddDays(-35), "Old revoked");

        db.RefreshTokens.AddRange(expiredToken, oldRevokedToken);
        await db.SaveChangesAsync();

        var countingUow = new CountingUnitOfWork(uow);
        var loginHandler = new LoginCommandHandler(userRepo, db, countingUow, hasher, jwtService);
        var response = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("purgetest", "Pass#123456"), "127.0.0.1"));

        Assert.NotNull(response);
        Assert.Equal(1, countingUow.CommitCount);

        // Đọc bằng DbContext mới để kiểm tra trong DB
        await using var verifyDb = new AppDbContext(options);
        var remainingTokens = await verifyDb.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();

        // 2 token cũ phải bị purge hoàn toàn khỏi database
        Assert.DoesNotContain(remainingTokens, t => t.Token == "expired-token-hash");
        Assert.DoesNotContain(remainingTokens, t => t.Token == "old-revoked-token-hash");

        // Chỉ còn token mới cấp từ login
        string newRawToken = response.RefreshToken;
        string newHash = RefreshTokenEntity.HashToken(newRawToken);
        Assert.Contains(remainingTokens, t => t.Token == newHash);
    }

    [Fact]
    public async Task Login_IssuesJwtWithCorrectRolesAndPermissions_AfterAssignUserRoles()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var userRepo = new UserRepository(db);
        var hasher = new Pbkdf2PasswordHasher();
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });

        var user = UserEntity.Create("jwtuser", "jwt@example.com", "JWT User", hasher.HashPassword("Pass#123456"));
        var roleLearner = new RoleEntity("LEARNER", "Learner", "Desc", isSystemRole: true);
        var roleTutor = new RoleEntity("TUTOR", "Tutor", "Desc", isSystemRole: false);

        var permListen = new PermissionEntity("tutor.listen", "Listen", "Tutor", "Desc");
        var permSpeak = new PermissionEntity("tutor.speak", "Speak", "Tutor", "Desc");
        roleTutor.GrantPermission(permListen.Id);
        roleTutor.GrantPermission(permSpeak.Id);

        db.Users.Add(user);
        db.Roles.AddRange(roleLearner, roleTutor);
        db.Permissions.AddRange(permListen, permSpeak);
        await db.SaveChangesAsync();

        // Bước 1: Gán vai trò TUTOR và LEARNER cho user qua AssignUserRoles
        var assignHandler = new AssignUserRolesCommandHandler(userRepo, db, uow);
        var assignResult = await assignHandler.HandleAsync(new AssignUserRolesCommand(user.Id, new AssignUserRolesRequest(new List<string> { "LEARNER", "TUTOR" })));

        Assert.Contains("LEARNER", assignResult.Roles);
        Assert.Contains("TUTOR", assignResult.Roles);
        Assert.Contains("tutor.listen", assignResult.Permissions);
        Assert.Contains("tutor.speak", assignResult.Permissions);

        // Bước 2: User đăng nhập -> phát AccessToken
        var loginHandler = new LoginCommandHandler(userRepo, db, uow, hasher, jwtService);
        var loginResponse = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("jwtuser", "Pass#123456"), "127.0.0.1"));

        // Bước 3: Validate AccessToken và kiểm tra claims
        var principal = jwtService.ValidateAccessToken(loginResponse.AccessToken);
        Assert.NotNull(principal);

        var rolesInClaims = principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permsInClaims = principal.FindAll("permission").Select(c => c.Value).ToList();

        Assert.Contains("LEARNER", rolesInClaims);
        Assert.Contains("TUTOR", rolesInClaims);
        Assert.Contains("tutor.listen", permsInClaims);
        Assert.Contains("tutor.speak", permsInClaims);
    }

    #endregion
}
