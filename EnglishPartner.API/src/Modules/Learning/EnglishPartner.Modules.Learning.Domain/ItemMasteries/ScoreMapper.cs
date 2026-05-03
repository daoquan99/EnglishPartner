namespace EnglishPartner.Modules.Learning.Domain.ItemMasteries;

/// <summary>
/// Maps external evaluation scores (e.g. from AI 0-100) to SM-2 Quality Score (0-5).
/// </summary>
public static class ScoreMapper
{
    /// <summary>
    /// Converts a 0-100 score to the SM-2 0-5 quality scale.
    /// </summary>
    /// <param name="rawScore">Score from 0 to 100.</param>
    /// <returns>Quality score from 0 to 5.</returns>
    public static int ToSm2QualityScore(int rawScore)
    {
        return rawScore switch
        {
            >= 90 => 5, // Perfect response
            >= 80 => 4, // Good response with minor hesitation/error
            >= 60 => 3, // Correct response but recalled with serious difficulty
            >= 40 => 2, // Incorrect response, but the correct one seemed easy to recall
            >= 20 => 1, // Incorrect response, the correct one remembered
            _     => 0  // Complete blackout
        };
    }
}
