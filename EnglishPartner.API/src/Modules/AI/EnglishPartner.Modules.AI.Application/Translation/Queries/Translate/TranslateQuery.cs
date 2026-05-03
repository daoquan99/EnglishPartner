using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Translation.Queries.Translate;

public sealed record TranslateQuery(string Text, string TargetLanguageCode) : IQuery<string>;
