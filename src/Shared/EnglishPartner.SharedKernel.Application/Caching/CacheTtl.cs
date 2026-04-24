using System;

namespace EnglishPartner.SharedKernel.Application.Caching;

/// <summary>
/// Centralized location for default Cache Time-To-Live (TTL) values.
/// </summary>
public static class CacheTtl
{
    public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Long = TimeSpan.FromHours(12);
    public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
    public static readonly TimeSpan ThirtyDays = TimeSpan.FromDays(30);

    public static class Identity
    {
        public static readonly TimeSpan PasswordResetToken = TimeSpan.FromMinutes(30);
    }

    public static class AI
    {
        public static readonly TimeSpan TtsAudio = ThirtyDays;
    }

    public static class Admin
    {
        public static readonly TimeSpan DashboardStats = OneMinute;
    }
}
