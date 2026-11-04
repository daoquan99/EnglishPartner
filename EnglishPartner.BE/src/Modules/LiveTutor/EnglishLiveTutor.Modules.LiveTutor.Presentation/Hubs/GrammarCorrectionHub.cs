using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Hubs;

/// <summary>
/// Dedicated Hub #4: Connects Frontend <-> Backend for Realtime Grammar & Vocabulary Correction (/hubs/grammar).
/// </summary>
[Authorize]
public sealed class GrammarCorrectionHub : Hub
{
    private static readonly HashSet<string> ValidCefrLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1", "A2", "B1", "B2", "C1", "C2"
    };

    private readonly ILiveSessionService _manager;
    private readonly IGrammarHubRateLimiter _rateLimiter;
    private readonly ILogger<GrammarCorrectionHub>? _logger;

    public GrammarCorrectionHub(
        ILiveSessionService manager,
        IGrammarHubRateLimiter rateLimiter,
        ILogger<GrammarCorrectionHub>? logger = null)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger;
    }

    private string GetUserId()
    {
        var user = Context.User;
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }
        return userId;
    }

    public async Task InitializeGrammarSession(StartSubHubSessionRequest request)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();

            if (!_rateLimiter.TryAcquireInitialize(userId))
            {
                throw new HubException("Bạn đã gửi quá nhiều yêu cầu khởi tạo phiên sửa ngữ pháp. Vui lòng thử lại sau.");
            }

            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(request.ConversationId);
            var normalizedReq = request with { ConversationId = normalizedConvId };

            await _manager.StartGrammarHubSessionAsync(userId, Context.ConnectionId, normalizedReq);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"grammar:{userId}:{normalizedConvId}");
            await Clients.Caller.SendAsync("GrammarHubReady", new
            {
                ConversationId = normalizedConvId
            });
        }, _logger, nameof(InitializeGrammarSession));
    }

    public async Task AnalyzeTurnGrammar(string conversationId, string turnId, string transcriptText, string cefrLevel)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(turnId) || turnId.Length > 64)
            {
                throw new HubException("turnId không hợp lệ hoặc vượt quá 64 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(transcriptText) || transcriptText.Length > 2000)
            {
                throw new HubException("transcriptText không hợp lệ hoặc vượt quá 2000 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(cefrLevel) || !ValidCefrLevels.Contains(cefrLevel.Trim()))
            {
                throw new HubException("cefrLevel không hợp lệ. Phải thuộc các cấp độ từ A1 đến C2.");
            }

            if (!_rateLimiter.TryAcquireAnalyze(userId))
            {
                throw new HubException("Bạn đã gửi quá nhiều yêu cầu phân tích ngữ pháp. Vui lòng thử lại sau.");
            }

            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            await _manager.AnalyzeTurnGrammarManualAsync(
                userId,
                Context.ConnectionId,
                normalizedConvId,
                turnId.Trim(),
                transcriptText.Trim(),
                cefrLevel.Trim().ToUpperInvariant(),
                Context.ConnectionAborted);
        }, _logger, nameof(AnalyzeTurnGrammar));
    }

    public async Task<FeedbackAudioResultDto> RequestFeedbackAudio(string conversationId, string correctionId, string variant)
    {
        return await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(correctionId) || correctionId.Length > 100)
            {
                throw new HubException("correctionId không hợp lệ hoặc vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(variant) ||
                (!string.Equals(variant, "corrected", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(variant, "improved", StringComparison.OrdinalIgnoreCase)))
            {
                throw new HubException("variant không hợp lệ (phải là 'corrected' hoặc 'improved').");
            }

            if (!_rateLimiter.TryAcquireFeedbackAudio(userId))
            {
                throw new HubException("Bạn đã gửi quá nhiều yêu cầu tạo âm thanh phản hồi. Vui lòng thử lại sau.");
            }

            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            try
            {
                return await _manager.RequestFeedbackAudioAsync(
                    userId,
                    Context.ConnectionId,
                    normalizedConvId,
                    correctionId.Trim(),
                    variant.Trim().ToLowerInvariant(),
                    Context.ConnectionAborted);
            }
            catch (FeedbackAudioException ex)
            {
                throw new HubException(ex.Code);
            }
        }, _logger, nameof(RequestFeedbackAudio));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _manager.OnClientDisconnectedAsync(Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi xử lý OnDisconnectedAsync trong GrammarCorrectionHub: {ExceptionType}", ex.GetType().Name);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
