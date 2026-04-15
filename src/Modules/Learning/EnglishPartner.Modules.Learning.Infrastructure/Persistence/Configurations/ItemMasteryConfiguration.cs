using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Configurations;

internal sealed class ItemMasteryConfiguration : IEntityTypeConfiguration<ItemMastery>
{
    public void Configure(EntityTypeBuilder<ItemMastery> builder)
    {
        builder.ToTable("item_masteries");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
        builder.HasIndex(x => x.NextReviewDate);
        builder.HasIndex(x => new { x.UserId, x.NextReviewDate }); // GetDueReviewsAsync composite

    }
}
