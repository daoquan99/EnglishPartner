using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.CreateFallingWordSession;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SubmitFallingWordAnswer;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class FallingWordGameCqrsPipelineTests
{
    private readonly CreateFallingWordSessionValidator _createValidator = new();
    private readonly SubmitFallingWordAnswerValidator _submitValidator = new();

    [Fact]
    public void CreateValidator_ValidPayload_PassesValidation()
    {
        var cmd = new CreateFallingWordSessionCommand(
            new CreateFallingWordSessionRequest(
                Source: "VocabularyCatalog",
                CefrLevel: "B1",
                PlayFilter: "Unplayed",
                TimeLimitSeconds: 5,
                RequestedCount: 10));

        var result = _createValidator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("InvalidSource", "All", "B1", 5, 10)]
    [InlineData("VocabularyCatalog", "FakeFilter", "B1", 5, 10)]
    [InlineData("VocabularyCatalog", "All", "Z9", 5, 10)]
    [InlineData("VocabularyCatalog", "All", "B1", 0, 10)]
    [InlineData("VocabularyCatalog", "All", "B1", 121, 10)]
    [InlineData("VocabularyCatalog", "All", "B1", 5, 0)]
    [InlineData("VocabularyCatalog", "All", "B1", 5, 101)]
    public void CreateValidator_InvalidInput_FailsValidation(
        string source, string filter, string? level, int timeLimit, int count)
    {
        var cmd = new CreateFallingWordSessionCommand(
            new CreateFallingWordSessionRequest(
                Source: source,
                CefrLevel: level,
                PlayFilter: filter,
                TimeLimitSeconds: timeLimit,
                RequestedCount: count));

        var result = _createValidator.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitValidator_EmptyAnswer_FailsValidation()
    {
        var cmd = new SubmitFallingWordAnswerCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SubmitFallingWordAnswerRequest("   "));

        var result = _submitValidator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.AnswerText");
    }

    [Fact]
    public void SubmitValidator_EmptySessionOrRoundId_FailsValidation()
    {
        var cmd = new SubmitFallingWordAnswerCommand(
            Guid.Empty,
            Guid.Empty,
            new SubmitFallingWordAnswerRequest("valid"));

        var result = _submitValidator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SessionId");
        Assert.Contains(result.Errors, e => e.PropertyName == "RoundId");
    }
}
