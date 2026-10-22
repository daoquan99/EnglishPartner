using FluentValidation;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SubmitFallingWordAnswer;

public sealed class SubmitFallingWordAnswerValidator : AbstractValidator<SubmitFallingWordAnswerCommand>
{
    public SubmitFallingWordAnswerValidator()
    {
        RuleFor(c => c.SessionId)
            .NotEmpty().WithMessage("SessionId không được để trống.");

        RuleFor(c => c.RoundId)
            .NotEmpty().WithMessage("RoundId không được để trống.");

        RuleFor(c => c.Request)
            .NotNull().WithMessage("Dữ liệu nộp bài không được để trống.");

        When(c => c.Request != null, () =>
        {
            RuleFor(c => c.Request.AnswerText)
                .NotEmpty().WithMessage("Từ trả lời không được để trống hoặc chỉ chứa khoảng trắng.")
                .MaximumLength(256).WithMessage("Từ trả lời không được vượt quá 256 ký tự.");
        });
    }
}
