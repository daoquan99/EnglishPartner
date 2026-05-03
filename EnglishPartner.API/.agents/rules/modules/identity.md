---
trigger: manual
---

# Module: Identity — Index

> **Scope:** Auth, Users, Roles, Permissions, Onboarding, Profile, Streaks  
> DB Schema: `identity`

---

## Tài liệu con

| File | Nội dung |
|---|---|
| [identity_auth.md](./identity/identity_auth.md) | Register, Login, Social Login, Refresh, Logout, Change Password, JWT/Session architecture |
| [identity_profile.md](./identity/identity_profile.md) | Onboarding, Profile (GET/PUT), External Login |
| [identity_streak.md](./identity/identity_streak.md) | Streak GET, Check-in |
| [identity_admin_user.md](./identity/identity_admin_user.md) | Admin: List/Activate/Deactivate/Delete/ResetPassword/ManageRoles cho User |
| [identity_admin_role.md](./identity/identity_admin_role.md) | Admin: CRUD Role, Assign/Remove Permission cho Role, List Permissions |

---

## Key Entities (tổng quan)

| Entity | Type | Notes |
|---|---|---|
| `User` | AggregateRoot | email, passwordHash, displayName, isOnboardingCompleted, IsActive |
| `UserProfile` | Entity (child of User) | nativeLanguage, targetLanguage, proficiencyLevel, learningGoal, dailyGoalMinutes, avatarUrl, dateOfBirth, morningReminderHour, eveningReminderHour, utcOffsetHours |
| `RefreshToken` | Entity (child of User) | token, expiresAt, isRevoked — stored in DB |
| `Streak` | Entity (child of User) | currentStreak, longestStreak, lastActivityDate, streakFreezes |
| `OnboardingAnswer` | Entity | userId, questionKey, answer, order |
| `Role` | AggregateRoot | name, description, permissions[] (many-to-many via RolePermission) |
| `Permission` | Entity | code (`module.resource.action`), name, module, description |
| `UserRole` | join entity | UserId + RoleId — **one user can have MANY roles** |
| `RolePermission` | join entity | RoleId + PermissionId — **one role can have MANY permissions** |
| `UserExternalLogin` | Entity (child of User) | loginProvider, providerKey, providerDisplayName |

---

## Domain Events

| DomainEvent | Raised by | → Integration Event |
|---|---|---|
| `UserRegisteredDomainEvent` | `User.Create()` | `UserRegisteredIntegrationEvent` → Learning, Notifications |
| `OnboardingCompletedDomainEvent` | `User.CompleteOnboarding(...)` | `OnboardingCompletedIntegrationEvent` → Learning |
| `UserProfileUpdatedDomainEvent` | `User.RaiseProfileUpdated(...)` | → Learning (sync preferences) |
| `StreakUpdatedDomainEvent` | CheckIn handler | → Notifications (streak milestone) |

---

## Error Codes (đầy đủ)

| Code | HTTP | When |
|---|---|---|
| `Identity.User.NotFound` | 404 | userId không tồn tại |
| `Identity.User.EmailAlreadyExists` | 409 | Email đã đăng ký |
| `Identity.User.InvalidCredentials` | 401 | Sai email/password |
| `Identity.User.Deactivated` | 401 | Tài khoản bị khóa |
| `Identity.User.OnboardingAlreadyCompleted` | 400 | Gọi complete-onboarding khi đã xong |
| `Identity.Token.InvalidRefreshToken` | 401 | Cookie hết hạn hoặc đã bị revoke |
| `Identity.Token.Expired` | 401 | Token đã hết hạn |
| `Identity.Session.NotFound` | 401 | Redis session không tồn tại |
| `Identity.Streak.AlreadyCheckedIn` | 400 | Đã check-in hôm nay rồi |
| `Identity.Streak.NotFound` | 404 | Streak record chưa tồn tại |
| `Identity.Role.NotFound` | 404 | Role không tồn tại |
| `Identity.Role.NameAlreadyExists` | 409 | Tên role đã tồn tại |
| `Identity.Role.HasUsers` | 400 | Không xóa được role đang có user |
| `Identity.Role.PermissionAlreadyAssigned` | 400 | Permission đã gán vào role rồi |
| `Identity.Role.PermissionNotAssigned` | 400 | Permission chưa được gán — không thể remove |
| `Identity.Permission.NotFound` | 404 | Permission id không tồn tại |
| `Identity.ExternalLogin.NotFound` | 404 | Provider chưa được liên kết |
| `Identity.ExternalLogin.CannotRemoveLast` | 400 | Không thể xóa phương thức đăng nhập cuối cùng |
| `Auth.SocialLogin.InvalidToken` | 400 | Social token Google không hợp lệ |

> **Frontend xử lý lỗi:** đọc `error.code` từ ProblemDetails response. Không chỉ check HTTP status.
