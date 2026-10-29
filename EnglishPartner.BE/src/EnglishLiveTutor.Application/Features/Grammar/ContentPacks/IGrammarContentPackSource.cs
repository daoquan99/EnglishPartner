namespace EnglishLiveTutor.Application.Features.Grammar.ContentPacks;

public sealed record GrammarContentPackDocument(string Name, string Json);

public interface IGrammarContentPackSource
{
    IReadOnlyList<GrammarContentPackDocument> LoadAll();
}
