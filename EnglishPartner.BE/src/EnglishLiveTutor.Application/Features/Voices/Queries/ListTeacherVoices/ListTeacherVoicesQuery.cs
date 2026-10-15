using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;

public sealed record ListTeacherVoicesQuery(
    bool OnlyEnabled = true,
    bool OnlyFeatured = false,
    string? Gender = null
) : IQuery<List<TeacherVoiceDto>>;

public sealed class ListTeacherVoicesQueryHandler : IQueryHandler<ListTeacherVoicesQuery, List<TeacherVoiceDto>>
{
    private readonly ITeacherVoiceRepository _voiceRepo;

    public ListTeacherVoicesQueryHandler(ITeacherVoiceRepository voiceRepo)
    {
        _voiceRepo = voiceRepo;
    }

    public async Task<List<TeacherVoiceDto>> HandleAsync(ListTeacherVoicesQuery query, CancellationToken ct = default)
    {
        var voices = await _voiceRepo.ListVoicesAsync(query.OnlyEnabled, query.OnlyFeatured, query.Gender, ct);
        return voices.Select(v => new TeacherVoiceDto(
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
    }
}
