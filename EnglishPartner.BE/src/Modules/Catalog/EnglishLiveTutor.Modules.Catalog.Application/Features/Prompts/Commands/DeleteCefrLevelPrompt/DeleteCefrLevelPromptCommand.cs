using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt;

public sealed record DeleteCefrLevelPromptCommand(string CefrLevel) : ICommand<bool>;
