using EnglishLiveTutor.Application.Features.Grammar.Rules;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarTheoryValidatorTests
{
    private static string BuildValidTheoryJson(bool isLevelReview = false)
    {
        string commonMistakes = isLevelReview
            ? "[]"
            : "[{\"wrong\":\"I hungry.\",\"right\":\"I am hungry.\",\"explanationVi\":\"Cần động từ to be.\"}]";

        return $$"""
        {
          "objectives": ["Hiểu cách dùng am/is/are."],
          "sections": [
            {
              "heading": "Cấu trúc cơ bản",
              "explanationVi": "To be biến đổi theo chủ ngữ.",
              "formulas": [
                { "label": "Khẳng định", "pattern": "S + am/is/are + ..." }
              ],
              "examples": [
                { "en": "I am a student.", "vi": "Tôi là học sinh.", "highlight": "am" }
              ],
              "notes": ["I đi với am."]
            }
          ],
          "commonMistakes": {{commonMistakes}},
          "tips": ["Hãy nhớ chia theo chủ ngữ."],
          "summary": ["Nắm vững 3 dạng am, is, are."]
        }
        """;
    }

    [Fact]
    public void Validate_ValidTheory_PassesWithoutErrors()
    {
        var json = BuildValidTheoryJson();
        var issues = GrammarTheoryValidator.Validate(json, isLevelReview: false);
        Assert.DoesNotContain(issues, i => i.Severity == "Error");
    }

    [Fact]
    public void Validate_MissingObjectives_ReturnsError()
    {
        var json = """
        {
          "objectives": [],
          "sections": [{ "heading": "H", "explanationVi": "E" }],
          "commonMistakes": [{ "wrong": "W", "right": "R", "explanationVi": "E" }],
          "summary": ["S"]
        }
        """;

        var issues = GrammarTheoryValidator.Validate(json);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Path.Contains("objectives"));
    }

    [Fact]
    public void Validate_ExampleMissingVi_ReturnsError()
    {
        var json = """
        {
          "objectives": ["Obj 1"],
          "sections": [
            {
              "heading": "H",
              "explanationVi": "E",
              "examples": [{ "en": "I am tall.", "vi": "" }]
            }
          ],
          "commonMistakes": [{ "wrong": "W", "right": "R", "explanationVi": "E" }],
          "summary": ["S"]
        }
        """;

        var issues = GrammarTheoryValidator.Validate(json);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Path.Contains(".vi"));
    }

    [Fact]
    public void Validate_StringTooLong_ReturnsError()
    {
        string longStr = new('x', 1005);
        var json = $$"""
        {
          "objectives": ["Obj 1"],
          "sections": [
            {
              "heading": "H",
              "explanationVi": "{{longStr}}"
            }
          ],
          "commonMistakes": [{ "wrong": "W", "right": "R", "explanationVi": "E" }],
          "summary": ["S"]
        }
        """;

        var issues = GrammarTheoryValidator.Validate(json);
        Assert.Contains(issues, i => i.Severity == "Error" && i.Path.Contains("explanationVi"));
    }

    [Fact]
    public void Validate_CommonMistakesEmpty_AllowedOnlyForLevelReview()
    {
        var reviewJson = BuildValidTheoryJson(isLevelReview: true);

        // Allowed for level review
        var reviewIssues = GrammarTheoryValidator.Validate(reviewJson, isLevelReview: true);
        Assert.DoesNotContain(reviewIssues, i => i.Severity == "Error");

        // Rejected for normal step
        var normalIssues = GrammarTheoryValidator.Validate(reviewJson, isLevelReview: false);
        Assert.Contains(normalIssues, i => i.Severity == "Error" && i.Path.Contains("commonMistakes"));
    }
}
