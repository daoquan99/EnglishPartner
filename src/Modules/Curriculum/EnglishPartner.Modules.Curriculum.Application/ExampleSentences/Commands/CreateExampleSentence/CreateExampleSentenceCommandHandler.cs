using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.CreateExampleSentence;

internal sealed class CreateExampleSentenceCommandHandler(
    IExampleSentenceRepository repository) : ICommandHandler<CreateExampleSentenceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExampleSentenceCommand request, CancellationToken cancellationToken)
    {
        var sentence = ExampleSentence.Create(
            request.Text,
            request.LanguageCode,
            request.AudioUrl,
            request.Note,
            request.Level,
            request.TopicId,
            request.VocabularyId,
            request.GrammarId);

        if (!string.IsNullOrWhiteSpace(request.Translation))
            sentence.AddTranslation("vi", request.Translation);

        repository.Add(sentence);
        return sentence.Id;
    }
}
