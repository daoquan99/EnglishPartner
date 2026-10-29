using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;

public sealed record ImportGrammarContentPackCommand : ICommand<GrammarContentPackImportResult>;

public sealed class ImportGrammarContentPackCommandHandler : ICommandHandler<ImportGrammarContentPackCommand, GrammarContentPackImportResult>
{
    private readonly GrammarContentPackImporter _importer;

    public ImportGrammarContentPackCommandHandler(GrammarContentPackImporter importer)
    {
        _importer = importer;
    }

    public async Task<GrammarContentPackImportResult> HandleAsync(ImportGrammarContentPackCommand command, CancellationToken ct = default)
    {
        return await _importer.ImportAsync(ct);
    }
}
