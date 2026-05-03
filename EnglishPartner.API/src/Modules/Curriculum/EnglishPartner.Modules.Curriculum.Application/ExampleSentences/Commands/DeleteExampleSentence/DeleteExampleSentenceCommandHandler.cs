using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.DeleteExampleSentence;

internal sealed class DeleteExampleSentenceCommandHandler(
    IExampleSentenceRepository repository,
    ICurrentUserService currentUser,
    ICacheService cache,
    TimeProvider timeProvider) : ICommandHandler<DeleteExampleSentenceCommand>
{
    public async Task<Result> Handle(DeleteExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sentence is null)
            return Result.Failure(CurriculumErrors.ExampleSentence.NotFound(request.Id));

        repository.Remove(sentence, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);

        await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.ExampleSentencesPrefix(sentence.LanguageCode), cancellationToken);

        return Result.Success();
    }
}
