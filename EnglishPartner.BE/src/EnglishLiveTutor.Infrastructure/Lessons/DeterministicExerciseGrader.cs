using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class DeterministicExerciseGrader : IDeterministicExerciseGrader
{
    private readonly IDictationScorer _dictationScorer;

    public DeterministicExerciseGrader(IDictationScorer dictationScorer)
    {
        _dictationScorer = dictationScorer;
    }

    public DeterministicGradeResult Grade(
        ExerciseType type,
        string contentJson,
        string? gradingDefinitionJson,
        string answerPayloadJson,
        decimal maxPoints)
    {
        var answerNode = ParseJson(answerPayloadJson);
        var gradingNode = ParseJson(gradingDefinitionJson ?? "{}");

        return type switch
        {
            ExerciseType.Flashcards => GradeFlashcards(answerNode, maxPoints),
            ExerciseType.MultipleChoice => GradeMultipleChoice(answerNode, gradingNode, maxPoints),
            ExerciseType.FillInTheBlanks => GradeFillInTheBlanks(answerNode, gradingNode, maxPoints),
            ExerciseType.Matching => GradeMatching(answerNode, gradingNode, maxPoints),
            ExerciseType.SpellingPractice => GradeSpellingPractice(answerNode, gradingNode, maxPoints),
            ExerciseType.SentenceUnscramble => GradeSentenceUnscramble(answerNode, gradingNode, maxPoints),
            ExerciseType.ErrorCorrection => GradeErrorCorrection(answerNode, gradingNode, maxPoints),
            ExerciseType.TrueFalse => GradeTrueFalse(answerNode, gradingNode, maxPoints),
            ExerciseType.Dictation => GradeDictation(answerNode, gradingNode, maxPoints),
            ExerciseType.ReadingComprehension or ExerciseType.ListeningComprehension => GradeComprehension(ParseJson(contentJson), answerNode, maxPoints),
            _ => throw new NotSupportedException($"Deterministic grading is not supported for exercise type '{type}'.")
        };
    }

    private static DeterministicGradeResult GradeFlashcards(JsonNode? answerNode, decimal maxPoints)
    {
        return new DeterministicGradeResult(
            IsCorrect: true,
            Score100: 100m,
            PointsEarned: 0m,
            MaxPoints: 0m,
            Explanation: "Flashcard self-assessment recorded.");
    }

    private static DeterministicGradeResult GradeMultipleChoice(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        var correctOptionIds = gradingNode?["correctOptionIds"]?.AsArray()
            .Select(x => x?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (correctOptionIds.Count == 0)
        {
            throw new InvalidOperationException("Câu hỏi trắc nghiệm chưa có đáp án đúng.");
        }

        var selectedOptionIds = answerNode?["selectedOptionIds"]?.AsArray()
            .Select(x => x?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? explanation = gradingNode?["explanation"]?.ToString();

        bool isMatch = selectedOptionIds.Count > 0 && correctOptionIds.SetEquals(selectedOptionIds);
        decimal score100 = isMatch ? 100m : 0m;
        decimal pointsEarned = isMatch ? maxPoints : 0m;

        return new DeterministicGradeResult(
            IsCorrect: isMatch,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: explanation);
    }

    private static DeterministicGradeResult GradeFillInTheBlanks(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        bool rootCaseSensitive = gradingNode?["caseSensitive"]?.GetValue<bool>() ?? false;
        bool rootIgnorePunctuation = gradingNode?["ignorePunctuation"]?.GetValue<bool>() ?? true;

        var blanksList = new List<(string Id, List<string> AcceptedAnswers, bool CaseSensitive, bool IgnorePunctuation)>();

        if (gradingNode?["blankAnswers"] is JsonObject blankAnswers)
        {
            foreach (var (bId, bDef) in blankAnswers)
            {
                var accepted = bDef?["acceptedAnswers"]?.AsArray()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList() ?? new List<string>();
                bool cs = bDef?["caseSensitive"]?.GetValue<bool>() ?? rootCaseSensitive;
                bool ip = bDef?["ignorePunctuation"]?.GetValue<bool>() ?? rootIgnorePunctuation;
                blanksList.Add((bId, accepted, cs, ip));
            }
        }
        else if (gradingNode?["correctAnswers"] is JsonArray correctAnswersArray)
        {
            foreach (var item in correctAnswersArray)
            {
                string bId = item?["blankId"]?.ToString() ?? item?["id"]?.ToString() ?? string.Empty;
                var accepted = item?["acceptedAnswers"]?.AsArray()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList() ?? new List<string>();
                bool cs = item?["caseSensitive"]?.GetValue<bool>() ?? rootCaseSensitive;
                bool ip = item?["ignorePunctuation"]?.GetValue<bool>() ?? rootIgnorePunctuation;
                if (!string.IsNullOrEmpty(bId))
                {
                    blanksList.Add((bId, accepted, cs, ip));
                }
            }
        }
        else if (gradingNode?["blanks"] is JsonArray blanksArray)
        {
            foreach (var item in blanksArray)
            {
                string bId = item?["id"]?.ToString() ?? item?["blankId"]?.ToString() ?? string.Empty;
                var accepted = (item?["correctAnswers"]?.AsArray() ?? item?["acceptedAnswers"]?.AsArray())?
                    .Select(x => x?.ToString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList() ?? new List<string>();
                bool cs = item?["caseSensitive"]?.GetValue<bool>() ?? rootCaseSensitive;
                bool ip = item?["ignorePunctuation"]?.GetValue<bool>() ?? rootIgnorePunctuation;
                if (!string.IsNullOrEmpty(bId))
                {
                    blanksList.Add((bId, accepted, cs, ip));
                }
            }
        }

        if (blanksList.Count == 0)
        {
            throw new InvalidOperationException("Bài điền từ chưa có đáp án chấm điểm.");
        }

        var userAnswers = answerNode?["answers"]?.AsObject();
        int totalBlanks = blanksList.Count;
        int correctCount = 0;

        foreach (var (blankId, acceptedAnswers, caseSensitive, ignorePunctuation) in blanksList)
        {
            string? userVal = null;
            if (userAnswers != null && userAnswers.TryGetPropertyValue(blankId, out var valNode))
            {
                userVal = valNode?.ToString();
            }

            if (userVal != null && IsBlankMatch(userVal, acceptedAnswers, caseSensitive, ignorePunctuation))
            {
                correctCount++;
            }
        }

        decimal accuracy = (decimal)correctCount / totalBlanks;
        decimal score100 = Math.Round(accuracy * 100m, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        return new DeterministicGradeResult(
            IsCorrect: correctCount == totalBlanks,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: $"{correctCount} of {totalBlanks} blanks correct.");
    }

    private static DeterministicGradeResult GradeMatching(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        var correctPairs = gradingNode?["correctPairs"]?.AsArray() ?? gradingNode?["pairs"]?.AsArray();

        if (correctPairs == null || correctPairs.Count == 0)
            throw new InvalidOperationException("Bài nối từ chưa có đáp án chấm điểm.");

        var correctMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cp in correctPairs)
        {
            string left = cp?["leftId"]?.ToString() ?? string.Empty;
            string right = cp?["rightId"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(left))
                correctMap[left] = right;
        }

        if (correctMap.Count == 0)
            throw new InvalidOperationException("Bài nối từ chưa có đáp án chấm điểm.");

        var userPairs = answerNode?["pairs"]?.AsArray();
        var userMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (userPairs != null)
        {
            foreach (var up in userPairs)
            {
                string left = up?["leftId"]?.ToString() ?? string.Empty;
                string right = up?["rightId"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right))
                {
                    userMap[left] = right; // Enforces unique leftId submitted
                }
            }
        }

        int totalPairs = correctMap.Count;
        int correctMatches = 0;

        foreach (var (left, expectedRight) in correctMap)
        {
            if (userMap.TryGetValue(left, out var actualRight) &&
                string.Equals(expectedRight, actualRight, StringComparison.OrdinalIgnoreCase))
            {
                correctMatches++;
            }
        }

        decimal accuracy = (decimal)correctMatches / totalPairs;
        decimal score100 = Math.Round(accuracy * 100m, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        return new DeterministicGradeResult(
            IsCorrect: correctMatches == totalPairs,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: $"{correctMatches} of {totalPairs} pairs correctly matched.");
    }

    private static DeterministicGradeResult GradeSpellingPractice(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        string targetWord = gradingNode?["targetWord"]?.ToString() ?? gradingNode?["correctAnswer"]?.ToString() ?? string.Empty;
        var acceptedSpellings = (gradingNode?["acceptedSpellings"]?.AsArray()
            ?? gradingNode?["acceptedAnswers"]?.AsArray()
            ?? gradingNode?["acceptedVariants"]?.AsArray())?
            .Select(x => x?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList() ?? new List<string>();

        if (!string.IsNullOrEmpty(targetWord) && !acceptedSpellings.Contains(targetWord, StringComparer.OrdinalIgnoreCase))
            acceptedSpellings.Add(targetWord);

        if (acceptedSpellings.Count == 0)
            throw new InvalidOperationException("Bài luyện chính tả chưa có từ mẫu.");

        bool caseSensitive = gradingNode?["caseSensitive"]?.GetValue<bool>() ?? false;
        string userWord = (answerNode?["spelledWord"]?.ToString() ?? string.Empty).Trim();

        bool isCorrect;
        if (caseSensitive)
        {
            isCorrect = acceptedSpellings.Any(s => string.Equals(s.Trim(), userWord, StringComparison.Ordinal));
        }
        else
        {
            string normUser = userWord.ToLowerInvariant();
            isCorrect = acceptedSpellings.Any(s => string.Equals(s.Trim().ToLowerInvariant(), normUser, StringComparison.Ordinal));
        }

        decimal score100 = isCorrect ? 100m : 0m;
        decimal pointsEarned = isCorrect ? maxPoints : 0m;

        return new DeterministicGradeResult(
            IsCorrect: isCorrect,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: isCorrect ? "Correct spelling!" : $"Expected spelling: {targetWord}.");
    }

    private static DeterministicGradeResult GradeSentenceUnscramble(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        var validSequences = gradingNode?["validSequences"]?.AsArray();
        if (validSequences == null || validSequences.Count == 0)
            throw new InvalidOperationException("Bài sắp xếp câu chưa có thứ tự mẫu.");

        var userSequence = answerNode?["orderedTokenIds"]?.AsArray()
            .Select(x => x?.ToString() ?? string.Empty)
            .ToList() ?? new List<string>();

        bool isMatch = false;
        foreach (var seq in validSequences)
        {
            var expected = seq?.AsArray()
                .Select(x => x?.ToString() ?? string.Empty)
                .ToList();

            if (expected != null && expected.SequenceEqual(userSequence, StringComparer.OrdinalIgnoreCase))
            {
                isMatch = true;
                break;
            }
        }

        decimal score100 = isMatch ? 100m : 0m;
        decimal pointsEarned = isMatch ? maxPoints : 0m;

        return new DeterministicGradeResult(
            IsCorrect: isMatch,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: isMatch ? "Correct sentence sequence!" : "Tokens are not in a valid grammatical sequence.");
    }

    private static DeterministicGradeResult GradeErrorCorrection(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        var acceptedCorrections = (gradingNode?["referenceCorrections"]?.AsArray()
            ?? gradingNode?["acceptedCorrections"]?.AsArray())?
            .Select(x => x?.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        if (acceptedCorrections == null || acceptedCorrections.Count == 0)
            throw new InvalidOperationException("Bài sửa lỗi chưa có câu sửa mẫu.");

        string userCorrection = answerNode?["correctedSentence"]?.ToString() ?? string.Empty;
        string normUser = NormalizeSentence(userCorrection);

        bool isMatch = acceptedCorrections.Any(ac => string.Equals(NormalizeSentence(ac), normUser, StringComparison.Ordinal));
        decimal score100 = isMatch ? 100m : 0m;
        decimal pointsEarned = isMatch ? maxPoints : 0m;

        return new DeterministicGradeResult(
            IsCorrect: isMatch,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: isMatch ? "Sentence correctly fixed!" : "Correction does not match accepted solutions.");
    }

    private static DeterministicGradeResult GradeTrueFalse(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        var correctAnswers = gradingNode?["statementAnswers"]?.AsObject() ?? gradingNode?["answers"]?.AsObject();

        if (correctAnswers == null || correctAnswers.Count == 0)
            throw new InvalidOperationException("Bài đúng/sai chưa có đáp án chấm điểm.");

        var userAnswers = answerNode?["answers"]?.AsObject();
        int total = correctAnswers.Count;
        int correctCount = 0;

        foreach (var (stmtId, valNode) in correctAnswers)
        {
            if (valNode is not JsonValue jVal || !jVal.TryGetValue<bool>(out var expected))
                continue;

            if (userAnswers != null && userAnswers.TryGetPropertyValue(stmtId, out var userValNode) &&
                userValNode is JsonValue uVal && uVal.TryGetValue<bool>(out var actual))
            {
                if (expected == actual)
                    correctCount++;
            }
        }

        decimal accuracy = total > 0 ? (decimal)correctCount / total : 0m;
        decimal score100 = Math.Round(accuracy * 100m, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        return new DeterministicGradeResult(
            IsCorrect: correctCount == total,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: maxPoints,
            Explanation: $"{correctCount} of {total} statements correct.");
    }

    private DeterministicGradeResult GradeDictation(
        JsonNode? answerNode,
        JsonNode? gradingNode,
        decimal maxPoints)
    {
        string refTranscript = gradingNode?["referenceTranscript"]?.ToString() ?? gradingNode?["transcript"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refTranscript))
            throw new InvalidOperationException("Bài chép chính tả chưa có transcript mẫu.");
        string learnerTranscript = answerNode?["transcribedText"]?.ToString() ?? string.Empty;

        var result = _dictationScorer.Score(refTranscript, learnerTranscript, maxPoints);

        return new DeterministicGradeResult(
            IsCorrect: result.WordAccuracy >= 0.99m,
            Score100: result.Score100,
            PointsEarned: result.PointsEarned,
            MaxPoints: maxPoints,
            Explanation: $"Accuracy: {result.Score100}% ({result.WordLevenshteinDistance} word edits out of {result.ReferenceWordCount} words).");
    }

    private static DeterministicGradeResult GradeComprehension(JsonNode? content, JsonNode? answer, decimal maxPoints)
    {
        var questions = content?["subQuestions"]?.AsArray()
            ?? throw new InvalidOperationException("Bài đọc/nghe chưa có câu hỏi.");
        if (questions.Count == 0) throw new InvalidOperationException("Bài đọc/nghe chưa có câu hỏi.");
        var submitted = answer?["subQuestionAnswers"]?.AsArray();
        var legacyAnswers = answer?["answers"] as JsonObject;
        decimal earned = 0, total = 0;
        foreach (var question in questions)
        {
            string id = question?["id"]?.ToString() ?? "";
            string kind = question?["questionKind"]?.ToString() ?? question?["kind"]?.ToString() ?? "";
            var definition = question?["grading"];
            var response = submitted?.FirstOrDefault(a => a?["questionId"]?.ToString() == id);
            var legacy = legacyAnswers?[id];
            decimal points = question?["maxPoints"]?.GetValue<decimal>() ?? 0m;
            total += points;
            bool correct;
            switch (kind)
            {
                case "SingleChoice":
                case "MultipleChoice":
                    var expected = definition?["correctOptionIds"]?.AsArray().Select(n => n!.ToString()).ToHashSet()
                        ?? throw new InvalidOperationException("Câu hỏi chưa có đáp án đúng.");
                    if (expected.Count == 0) throw new InvalidOperationException("Câu hỏi chưa có đáp án đúng.");
                    var selected = response?["selectedOptionIds"] as JsonArray ?? legacy as JsonArray;
                    var actual = selected?.Select(n => n!.ToString()).ToHashSet() ??
                        (legacy is JsonValue ? new HashSet<string> { legacy.ToString() } : new HashSet<string>());
                    correct = expected.SetEquals(actual);
                    break;
                case "TrueFalse":
                    bool expectedBoolean = definition?["statementAnswer"]?.GetValue<bool>()
                        ?? throw new InvalidOperationException("Câu đúng/sai chưa có đáp án.");
                    var booleanAnswer = response?["answer"] ?? legacy;
                    correct = booleanAnswer is JsonValue v && v.TryGetValue<bool>(out var value) && value == expectedBoolean;
                    break;
                case "FillBlank":
                    var accepted = definition?["acceptedAnswers"]?.AsArray()
                        ?? throw new InvalidOperationException("Câu điền từ chưa có đáp án.");
                    correct = IsBlankMatch(response?["textAnswer"]?.ToString() ?? legacy?.ToString() ?? "",
                        accepted.Select(n => n!.ToString()), false, true);
                    break;
                case "ShortAnswer":
                    throw new NotSupportedException("Câu trả lời mở trong bài đọc/nghe cần được chấm bằng AI; chưa có kết quả chấm.");
                default:
                    throw new InvalidOperationException("Loại câu hỏi đọc/nghe không hợp lệ.");
            }
            if (correct) earned += points;
        }
        if (total <= 0) throw new InvalidOperationException("Tổng điểm câu hỏi phải lớn hơn 0.");
        decimal score = Math.Round(earned / total * 100m, 2, MidpointRounding.AwayFromZero);
        return new DeterministicGradeResult(earned == total, score,
            Math.Round(score / 100m * maxPoints, 2, MidpointRounding.AwayFromZero), maxPoints,
            $"Đạt {earned}/{total} điểm câu hỏi.");
    }

    private static bool IsBlankMatch(
        string userVal,
        IEnumerable<string> correctAnswers,
        bool caseSensitive,
        bool ignorePunctuation)
    {
        string u = userVal.Trim();
        if (!caseSensitive)
            u = u.ToLowerInvariant();
        if (ignorePunctuation)
            u = u.Trim('.', ',', '!', '?', ';', ':', '\'', '"');

        foreach (var ca in correctAnswers)
        {
            string expected = ca.Trim();
            if (!caseSensitive)
                expected = expected.ToLowerInvariant();
            if (ignorePunctuation)
                expected = expected.Trim('.', ',', '!', '?', ';', ':', '\'', '"');

            if (string.Equals(u, expected, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string NormalizeSentence(string s)
    {
        return s.Trim().ToLowerInvariant().Trim('.', '!', '?');
    }

    private static JsonNode? ParseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }
}
