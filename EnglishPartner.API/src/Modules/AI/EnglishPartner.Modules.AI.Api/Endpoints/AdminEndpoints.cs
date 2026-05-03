using EnglishPartner.Modules.AI.Application.Admin.Queries.GetAiRequestLogs;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class AdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/request-logs", async (
            [AsParameters] GetAiRequestLogsRequest request,
            ISender sender) =>
        {
            var query = new GetAiRequestLogsQuery(
                request.Page,
                request.PageSize,
                request.RequestType,
                request.IsSuccess,
                request.From,
                request.To);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetAiRequestLogs")
        .WithSummary("Get a paginated list of AI request logs.");
    }
}

public sealed record GetAiRequestLogsRequest(
    int Page = 1,
    int PageSize = 20,
    string? RequestType = null,
    bool? IsSuccess = null,
    DateTime? From = null,
    DateTime? To = null);
