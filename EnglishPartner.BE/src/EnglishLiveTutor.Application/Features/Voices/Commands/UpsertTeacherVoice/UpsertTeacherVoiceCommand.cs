using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;

public sealed record UpsertTeacherVoiceCommand(
    string Id,
    UpsertTeacherVoiceRequest Request
) : ICommand<TeacherVoiceDto>;
