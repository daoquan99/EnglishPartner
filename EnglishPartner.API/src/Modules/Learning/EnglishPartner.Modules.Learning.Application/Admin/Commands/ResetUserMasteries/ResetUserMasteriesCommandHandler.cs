using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.ResetUserMasteries;

internal sealed class ResetUserMasteriesCommandHandler(
    IItemMasteryRepository masteryRepository) : ICommandHandler<ResetUserMasteriesCommand>
{
    public async Task<Result> Handle(ResetUserMasteriesCommand request, CancellationToken cancellationToken)
    {
        await masteryRepository.DeleteAllByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}
