using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Commands.CreateLanguage;

public sealed record CreateLanguageCommand(string Code, string Name, string NativeName, int SortOrder) : ICommand<Guid>;
