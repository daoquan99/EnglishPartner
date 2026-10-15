using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;

public sealed record CreateAiModelConfigCommand(UpsertAiModelConfigRequest Request) : ICommand<AiModelConfigEntity>;
