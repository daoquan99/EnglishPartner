using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.DraftGrammarTheoryWithAi;

public sealed record DraftGrammarTheoryWithAiCommand(
    string StepCode,
    DraftGrammarTheoryWithAiRequest Request
) : ICommand<DraftGrammarTheoryWithAiResponse>;
