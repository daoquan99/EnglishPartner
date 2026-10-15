using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;

public sealed class ListSpeechSpeedsQueryHandler(IAppDbContextFactory dbFactory) : IQueryHandler<ListSpeechSpeedsQuery, PagedResultDto<SpeechSpeedOptionDto>>
{
    public async Task<PagedResultDto<SpeechSpeedOptionDto>> HandleAsync(ListSpeechSpeedsQuery request, CancellationToken ct = default)
    {
        var onlyActive = request.OnlyActive;
        var search = request.Search;
        var page = request.Page;
        var pageSize = request.PageSize;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.SpeechSpeeds.AsNoTracking().AsQueryable();
        if (onlyActive ?? true)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.Code.ToLower().Contains(term) ||
                s.Label.ToLower().Contains(term) ||
                s.DescriptionVi.ToLower().Contains(term) ||
                s.RecommendedCefrRange.ToLower().Contains(term));
        }

        var projected = query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SpeedValue)
            .ThenBy(s => s.Id)
            .Select(s => new SpeechSpeedOptionDto(
                s.Id,
                s.Code,
                s.SpeedValue,
                s.Label,
                s.DescriptionVi,
                s.RecommendedCefrRange,
                s.IsDefault,
                s.IsActive,
                s.DisplayOrder));

        return await PagedResultDto<SpeechSpeedOptionDto>.CreateAsync(projected, page, pageSize, ct);
    }
}
