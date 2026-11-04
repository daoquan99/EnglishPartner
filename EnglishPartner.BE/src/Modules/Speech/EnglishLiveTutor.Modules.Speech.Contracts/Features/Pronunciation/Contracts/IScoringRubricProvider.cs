namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record ScoringRubricCriterionSnapshot(
    string Code,
    string Name,
    string Description,
    decimal Weight,
    int DisplayOrder,
    bool IsEnabled,
    string DescriptorLevel0,
    string DescriptorLevel1,
    string DescriptorLevel2,
    string DescriptorLevel3,
    string DescriptorLevel4);

public sealed record ScoringRubricSnapshot(
    string Code,
    int Version,
    IReadOnlyList<ScoringRubricCriterionSnapshot> Criteria);

public interface IScoringRubricProvider
{
    Task<ScoringRubricSnapshot?> GetByCodeAndVersionAsync(
        string code,
        int version,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListExistingCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default);
}
