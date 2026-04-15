using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.DeleteVocabulary;

public sealed record DeleteVocabularyCommand(Guid Id) : ICommand;
