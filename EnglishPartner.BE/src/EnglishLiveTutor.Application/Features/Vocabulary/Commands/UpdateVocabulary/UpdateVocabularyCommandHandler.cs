using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;

public sealed class UpdateVocabularyCommandHandler(IVocabularyRepository repository)
    : ICommandHandler<UpdateVocabularyCommand, VocabularyFlashcardDto?>
{
    public async Task<VocabularyFlashcardDto?> HandleAsync(UpdateVocabularyCommand command, CancellationToken ct = default)
    {
        var vocabulary = await repository.GetByIdWithDetailsAsync(command.Id, ct);
        if (vocabulary is null) return null;

        var req = command.Request;
        vocabulary.UpdateDetails(req.Word, req.FrequencyRank, req.TopicCategory);
        // Preserve the sense referenced by daily plans and learning progress instead of deleting the aggregate.
        var sense = vocabulary.Senses.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayOrder).FirstOrDefault();
        repository.RemoveFlashcardDetails(vocabulary, sense);
        vocabulary.ClearPronunciationsAndWordFamilies();
        sense?.ClearRelatedContent();

        sense = VocabularyAdminEditor.Apply(vocabulary, req, sense);
        await repository.SaveChangesAsync(ct);
        return VocabularyProjectionHelper.MapToFlashcardDto(Guid.Empty, vocabulary, sense, "New", "Pending", null, null);
    }
}
