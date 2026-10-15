using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;

public sealed class ListTeacherVoicesQueryHandler : IQueryHandler<ListTeacherVoicesQuery, PagedResultDto<TeacherVoiceDto>>
{
    private readonly ITeacherVoiceRepository _voiceRepo;

    public ListTeacherVoicesQueryHandler(ITeacherVoiceRepository voiceRepo)
    {
        _voiceRepo = voiceRepo;
    }

    public async Task<PagedResultDto<TeacherVoiceDto>> HandleAsync(ListTeacherVoicesQuery query, CancellationToken ct = default)
    {
        var (voices, totalCount) = await _voiceRepo.ListPagedVoicesAsync(
            query.OnlyEnabled,
            query.OnlyFeatured,
            query.Gender,
            query.Search,
            query.Page,
            query.PageSize,
            ct);

        var dtos = voices.Select(v => new TeacherVoiceDto(
            v.Id,
            v.Name,
            v.DisplayName,
            v.Label,
            v.Type,
            v.Gender,
            v.Persona,
            v.Style,
            v.Description,
            v.IsFeatured,
            v.IsEnabled,
            v.DisplayOrder
        )).ToList();

        return PagedResultDto<TeacherVoiceDto>.FromPage(dtos, totalCount, query.Page, query.PageSize);
    }
}
