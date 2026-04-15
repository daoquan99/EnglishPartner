using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.UpdateExampleSentence;

internal sealed class UpdateExampleSentenceCommandHandler(
    IExampleSentenceRepository repository) : ICommandHandler<UpdateExampleSentenceCommand>
{
    public async Task<Result> Handle(UpdateExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sentence is null) return Result.Failure(CurriculumErrors.ExampleSentence.NotFound(request.Id));

        sentence.Update(
            request.Text,
            request.AudioUrl,
            request.Note,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId);

        if (!string.IsNullOrWhiteSpace(request.Translation))
            sentence.AddTranslation("vi", request.Translation);

        repository.Update(sentence);
        return Result.Success();
    }
}
