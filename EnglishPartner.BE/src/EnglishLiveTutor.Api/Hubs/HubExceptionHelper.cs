using EnglishLiveTutor.Application.Common.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Hubs;

public static class HubExceptionHelper
{
    public const string DefaultNotFoundMessage = "Không tìm thấy hội thoại.";
    public const string DefaultErrorMessage = "Đã xảy ra lỗi trong quá trình xử lý.";

    public static async Task ExecuteAsync(Func<Task> action, ILogger? logger = null, string? operation = null)
    {
        try
        {
            await action();
        }
        catch (NotFoundException)
        {
            throw new HubException(DefaultNotFoundMessage);
        }
        catch (ForbiddenException)
        {
            throw new HubException(DefaultNotFoundMessage);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogError("Lỗi khi thực hiện {Operation}: {ExceptionType}", operation ?? "thao tác", ex.GetType().Name);
            throw new HubException(DefaultErrorMessage);
        }
    }
}
