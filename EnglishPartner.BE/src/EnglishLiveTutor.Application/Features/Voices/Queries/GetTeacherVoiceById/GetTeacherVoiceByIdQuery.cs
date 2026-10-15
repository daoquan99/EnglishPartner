using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.GetTeacherVoiceById;

public sealed record GetTeacherVoiceByIdQuery(string Id) : IQuery<TeacherVoiceDto?>;

public sealed class GetTeacherVoiceByIdQueryHandler : IQueryHandler<GetTeacherVoiceByIdQuery, TeacherVoiceDto?>
{
    private readonly ITeacherVoiceRepository _voiceRepo;

    public GetTeacherVoiceByIdQueryHandler(ITeacherVoiceRepository voiceRepo)
    {
        _voiceRepo = voiceRepo;
    }

    public async Task<TeacherVoiceDto?> HandleAsync(GetTeacherVoiceByIdQuery query, CancellationToken ct = default)
    {
        var v = await _voiceRepo.GetByIdAsync(query.Id, ct);
        if (v is null) return null;

        return new TeacherVoiceDto(
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
            v.DisplayOrder);
    }
}
