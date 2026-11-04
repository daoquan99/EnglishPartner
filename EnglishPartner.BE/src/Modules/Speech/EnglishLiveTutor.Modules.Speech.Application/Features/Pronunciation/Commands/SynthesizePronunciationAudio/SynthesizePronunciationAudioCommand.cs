using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;

public sealed record SynthesizePronunciationAudioCommand(SynthesizePronunciationAudioRequest Request)
    : ICommand<SynthesizePronunciationAudioResponseDto>;
