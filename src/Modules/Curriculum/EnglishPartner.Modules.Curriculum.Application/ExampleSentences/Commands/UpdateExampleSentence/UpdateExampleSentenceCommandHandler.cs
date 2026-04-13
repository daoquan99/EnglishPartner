using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using Error = EnglishPartner.SharedKernel.Results.Error;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.UpdateExampleSentence;

internal sealed class UpdateExampleSentenceCommandHandler(
    IExampleSentenceRepository repository) : ICommandHandler<UpdateExampleSentenceCommand>
{
    public async Task<Result> Handle(UpdateExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (sentence is null) return Result.Failure(new Error("ExampleSentence.NotFound", $"ExampleSentence with ID '{request.Id}' was not found."));

        sentence.Update(
            request.Text,
            request.Translation,
            request.AudioUrl,
            request.Note,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId);

        repository.Update(sentence);
        return Result.Success();
    }
}
