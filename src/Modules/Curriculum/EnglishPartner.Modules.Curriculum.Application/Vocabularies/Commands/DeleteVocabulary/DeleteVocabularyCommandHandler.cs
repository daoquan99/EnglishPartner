using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.DeleteVocabulary;

internal sealed class DeleteVocabularyCommandHandler(
    IVocabularyRepository vocabularyRepository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    ICacheService cache) : ICommandHandler<DeleteVocabularyCommand>
{
    public async Task<Result> Handle(DeleteVocabularyCommand request, CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (vocabulary is null)
            return Result.Failure(CurriculumErrors.Vocabulary.NotFound(request.Id));

        vocabularyRepository.Remove(vocabulary, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        
        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(vocabulary.LanguageCode), cancellationToken);
        
        return Result.Success();
    }
}
