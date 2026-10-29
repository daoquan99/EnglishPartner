using System.Reflection;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;

namespace EnglishLiveTutor.Infrastructure.Grammar;

public sealed class EmbeddedGrammarContentPackSource : IGrammarContentPackSource
{
    public IReadOnlyList<GrammarContentPackDocument> LoadAll()
    {
        var assembly = typeof(EmbeddedGrammarContentPackSource).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Grammar.ContentPacks.", StringComparison.OrdinalIgnoreCase) &&
                           name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var docs = new List<GrammarContentPackDocument>(resourceNames.Count);

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            docs.Add(new GrammarContentPackDocument(resourceName, json));
        }

        return docs;
    }
}
