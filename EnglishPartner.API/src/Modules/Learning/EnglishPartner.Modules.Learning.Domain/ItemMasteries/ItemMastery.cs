using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.ItemMasteries;

public enum ItemType
{
    Vocabulary = 0,
    Grammar = 1,
    ExampleSentence = 2,
    Topic = 3,
    Conversation = 4,
    Exercise = 5
}

/// <summary>
/// Implements SM-2 Spaced Repetition tracking for a specific curriculum item.
/// </summary>
public sealed class ItemMastery : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid ItemId { get; private set; }
    public ItemType Type { get; private set; }
    
    // SuperMemo-2 Variables
    public double EasinessFactor { get; private set; }
    public int IntervalDays { get; private set; }
    public int Repetitions { get; private set; }
    public DateTime NextReviewDate { get; private set; }

    private ItemMastery() { }

    public static ItemMastery Create(Guid userId, Guid itemId, ItemType type, DateTime utcNow)
    {
        return new ItemMastery
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId,
            Type = type,
            EasinessFactor = 2.5, // Default SM-2 starting EF
            IntervalDays = 1,     // Initial interval 1 day
            Repetitions = 0,
            NextReviewDate = utcNow.Date
        };
    }

    /// <summary>
    /// Evaluates the item using the SM-2 algorithm.
    /// </summary>
    /// <param name="quality">A rating from 0 to 5. (0=Blackout, 3=Hard, 4=Good, 5=Perfect)</param>
    public void Review(int quality, DateTime reviewDate)
    {
        if (quality < 0 || quality > 5) throw new ArgumentOutOfRangeException(nameof(quality));

        if (quality < 3)
        {
            Repetitions = 0;
            IntervalDays = 1;
        }
        else
        {
            if (Repetitions == 0) IntervalDays = 1;
            else if (Repetitions == 1) IntervalDays = 6;
            else IntervalDays = (int)Math.Round(IntervalDays * EasinessFactor);

            Repetitions++;
        }

        EasinessFactor += (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
        if (EasinessFactor < 1.3) EasinessFactor = 1.3;

        NextReviewDate = reviewDate.Date.AddDays(IntervalDays);
    }
}
