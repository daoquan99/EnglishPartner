using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.Admin.Commands.AdjustUserXp;

public sealed record AdjustUserXpCommand(
    Guid   UserId,
    int    Amount,
    string Reason) : ICommand<AdjustXpResult>;

public sealed record AdjustXpResult(int NewTotalXp, string NewLevel);

