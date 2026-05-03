using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.UpdateLanguage;

public sealed record UpdateLanguageCommand(Guid Id, string Name, string NativeName, int SortOrder) : ICommand;
