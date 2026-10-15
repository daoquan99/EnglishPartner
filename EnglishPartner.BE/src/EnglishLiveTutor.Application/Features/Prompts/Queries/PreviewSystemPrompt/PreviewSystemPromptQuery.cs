using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;

public sealed record PreviewSystemPromptQuery(BuildSystemPromptPreviewRequest Request) : IQuery<string>;
