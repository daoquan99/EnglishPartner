using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;

public sealed record ReviewItemCommand(
    Guid UserId,
    Guid ItemId,
    ItemType Type,
    int QualityRating) : ICommand;
