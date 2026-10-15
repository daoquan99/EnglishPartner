using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;

public sealed class CreateVocabularyCommandHandler(IVocabularyRepository repository)
    : ICommandHandler<CreateVocabularyCommand, VocabularyFlashcardDto>
{
    public async Task<VocabularyFlashcardDto> HandleAsync(CreateVocabularyCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Word))
            throw new InvalidOperationException("Từ vựng (Word) không được để trống.");

        var vocabulary = VocabularyEntity.Create(req.Word, sourceType: "AdminCreated",
            frequencyRank: req.FrequencyRank, topicCategory: req.TopicCategory);
        var sense = VocabularyAdminEditor.Apply(vocabulary, req);
        await repository.AddAsync(vocabulary, ct);
        await repository.SaveChangesAsync(ct);
        return VocabularyProjectionHelper.MapToFlashcardDto(Guid.Empty, vocabulary, sense, "New", "Pending", null, null);
    }
}
