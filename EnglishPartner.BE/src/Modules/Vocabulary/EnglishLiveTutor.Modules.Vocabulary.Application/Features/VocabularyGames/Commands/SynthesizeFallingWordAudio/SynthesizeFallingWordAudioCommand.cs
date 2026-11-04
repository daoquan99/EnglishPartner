using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SynthesizeFallingWordAudio;

public sealed record SynthesizeFallingWordAudioRequest(
    string? ApiKey = null,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string Accent = "US",
    string ModelCode = "gemini-3.8-live",
    string? CardId = null);

public sealed record FallingWordAudioDto(
    string AudioBase64,
    string MimeType,
    int SampleRate,
    int DurationMs);

public sealed record SynthesizeFallingWordAudioCommand(
    Guid SessionId,
    Guid RoundId,
    SynthesizeFallingWordAudioRequest Request) : ICommand<FallingWordAudioDto>;
