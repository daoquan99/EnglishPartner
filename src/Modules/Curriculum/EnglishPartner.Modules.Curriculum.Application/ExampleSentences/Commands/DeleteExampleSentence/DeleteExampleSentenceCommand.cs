using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.DeleteExampleSentence;

public sealed record DeleteExampleSentenceCommand(Guid Id) : ICommand;
