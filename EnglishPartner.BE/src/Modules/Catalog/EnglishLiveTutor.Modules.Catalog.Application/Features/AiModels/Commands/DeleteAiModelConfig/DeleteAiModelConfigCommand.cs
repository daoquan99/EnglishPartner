using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;

public sealed record DeleteAiModelConfigCommand(Guid Id) : ICommand<bool>;
