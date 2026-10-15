using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Api.Endpoints;

public static class PaginationEndpointExtensions
{
    /// <summary>
    /// Writes pagination metadata headers (X-Total-Count, X-Page, X-Page-Size, X-Total-Pages)
    /// from an already database-paged PagedResultDto and returns either the PagedResultDto
    /// envelope (when paged=true) or the paged items list for backward compatibility.
    /// </summary>
    public static IResult ToHttpResult<T>(
        this PagedResultDto<T> pagedResult,
        HttpContext httpContext,
        bool? paged = null)
    {
        httpContext.Response.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
        httpContext.Response.Headers["X-Page"] = pagedResult.Page.ToString();
        httpContext.Response.Headers["X-Page-Size"] = pagedResult.PageSize.ToString();
        httpContext.Response.Headers["X-Total-Pages"] = pagedResult.TotalPages.ToString();

        if (paged == true)
        {
            return Results.Ok(pagedResult);
        }

        return Results.Ok(pagedResult.Items);
    }

    /// <summary>
    /// Executes database-level CountAsync and Skip/Take pagination on an IQueryable<T>
    /// and formats the HTTP response with pagination headers.
    /// </summary>
    public static async Task<IResult> ToPaginatedOrListResultAsync<T>(
        this IQueryable<T> query,
        HttpContext httpContext,
        int? page = null,
        int? pageSize = null,
        bool? paged = null,
        CancellationToken ct = default)
    {
        var pagedResult = await PagedResultDto<T>.CreateAsync(query, page, pageSize, ct);
        return pagedResult.ToHttpResult(httpContext, paged);
    }
}
