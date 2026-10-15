using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;

public sealed record UpdateAiModelConfigCommand(Guid Id, UpsertAiModelConfigRequest Request) : ICommand<AiModelConfigEntity?>;
