using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;

public sealed record DeleteSpeechSpeedCommand(Guid Id) : ICommand<bool>;
