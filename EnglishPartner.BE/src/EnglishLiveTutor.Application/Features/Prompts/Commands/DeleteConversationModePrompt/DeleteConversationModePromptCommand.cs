using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts.Repositories;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;

public sealed record DeleteConversationModePromptCommand(string ModeCode) : ICommand<bool>;
