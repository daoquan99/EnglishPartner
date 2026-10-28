namespace EnglishLiveTutor.Api.Configuration;

public static class SignalRConfigurationExtensions
{
    public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddAppSignalR(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var signalR = services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 512 * 1024;
            options.EnableDetailedErrors = isDevelopment;
        });

        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalR.AddStackExchangeRedis(redisConnectionString, o =>
            {
                o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("englishpartner");
            });
        }

        return signalR;
    }
}
