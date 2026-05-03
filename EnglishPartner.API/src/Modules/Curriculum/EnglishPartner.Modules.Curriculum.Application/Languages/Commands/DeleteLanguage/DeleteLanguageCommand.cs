using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.DeleteLanguage;

public sealed record DeleteLanguageCommand(Guid Id) : ICommand;
