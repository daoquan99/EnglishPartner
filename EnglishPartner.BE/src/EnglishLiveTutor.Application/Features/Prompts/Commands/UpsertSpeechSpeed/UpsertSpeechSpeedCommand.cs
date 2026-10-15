using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;

public sealed record UpsertSpeechSpeedCommand(UpsertSpeechSpeedRequest Request) : ICommand<SpeechSpeedOptionDto>;
