using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.DeleteItemMastery;

internal sealed class DeleteItemMasteryCommandHandler(
    IItemMasteryRepository masteryRepository) : ICommandHandler<DeleteItemMasteryCommand>
{
    public async Task<Result> Handle(DeleteItemMasteryCommand request, CancellationToken cancellationToken)
    {
        var deleted = await masteryRepository.DeleteByItemIdAsync(request.UserId, request.ItemId, cancellationToken);
        if (deleted == 0)
            return Result.Failure(new Error("Learning.Mastery.NotFound",
                $"No mastery record found for user '{request.UserId}' and item '{request.ItemId}'."));

        return Result.Success();
    }
}
