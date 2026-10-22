using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SynthesizeFallingWordAudio;

public sealed record SynthesizeFallingWordAudioCommand(
    Guid SessionId,
    Guid RoundId,
    SynthesizeExerciseAudioRequest Request) : ICommand<ExerciseAudioDto>;
