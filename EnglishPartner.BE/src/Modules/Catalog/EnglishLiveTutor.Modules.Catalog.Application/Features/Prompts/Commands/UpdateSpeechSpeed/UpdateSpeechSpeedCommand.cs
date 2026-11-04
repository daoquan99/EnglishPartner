using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;

public sealed record UpdateSpeechSpeedCommand(Guid Id, UpsertSpeechSpeedRequest Request) : ICommand<SpeechSpeedOptionDto?>;
