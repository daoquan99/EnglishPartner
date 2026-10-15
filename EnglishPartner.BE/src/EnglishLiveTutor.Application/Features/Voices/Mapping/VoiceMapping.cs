using EnglishLiveTutor.Application.Features.Voices.Contracts;
using EnglishLiveTutor.Domain.Voices;

namespace EnglishLiveTutor.Application.Features.Voices.Mapping;

public static class VoiceMapping
{
    public static TeacherVoiceDto ToDto(this TeacherVoiceEntity v) => new(
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
    );
}
