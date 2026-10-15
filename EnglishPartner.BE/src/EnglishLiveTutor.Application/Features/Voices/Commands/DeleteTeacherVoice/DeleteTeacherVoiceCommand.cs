using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Voices.Repositories;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;

public sealed record DeleteTeacherVoiceCommand(string Id) : ICommand<bool>;
