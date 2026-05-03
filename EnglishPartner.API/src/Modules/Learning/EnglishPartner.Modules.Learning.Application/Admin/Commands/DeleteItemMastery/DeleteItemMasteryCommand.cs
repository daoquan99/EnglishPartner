using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.DeleteItemMastery;

public sealed record DeleteItemMasteryCommand(Guid UserId, Guid ItemId) : ICommand;
