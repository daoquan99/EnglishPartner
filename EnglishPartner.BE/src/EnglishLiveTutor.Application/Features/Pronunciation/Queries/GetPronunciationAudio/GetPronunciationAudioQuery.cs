using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;

public sealed record GetPronunciationAudioQuery(
    string TargetType,
    Guid TargetId,
    string? VoiceName = null,
    double? SpeechSpeed = null,
    string? Accent = null,
    string? ModelCode = null,
    Guid? VocabularyId = null,
    Guid? SenseId = null
) : IQuery<SynthesizePronunciationAudioResponseDto?>;
