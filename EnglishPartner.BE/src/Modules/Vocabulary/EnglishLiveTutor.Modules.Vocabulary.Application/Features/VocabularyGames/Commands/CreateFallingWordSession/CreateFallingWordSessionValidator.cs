using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using FluentValidation;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.CreateFallingWordSession;

public sealed class CreateFallingWordSessionValidator : AbstractValidator<CreateFallingWordSessionCommand>
{
    private static readonly HashSet<string> AllowedCefrLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1", "A2", "B1", "B2", "C1", "C2"
    };

    public CreateFallingWordSessionValidator()
    {
        RuleFor(c => c.Request).NotNull().WithMessage("Dữ liệu yêu cầu không được để trống.");

        When(c => c.Request != null, () =>
        {
            RuleFor(c => c.Request.Source)
                .NotEmpty().WithMessage("Nguồn từ vựng không được để trống.")
                .Must(s => Enum.TryParse<FallingWordGameSource>(s, true, out _))
                .WithMessage("Nguồn từ vựng không hợp lệ. Cho phép: VocabularyCatalog, DailyVocabulary.");

            RuleFor(c => c.Request.PlayFilter)
                .NotEmpty().WithMessage("Bộ lọc lượt chơi không được để trống.")
                .Must(f => Enum.TryParse<FallingWordPlayFilter>(f, true, out _))
                .WithMessage("Bộ lọc lượt chơi không hợp lệ. Cho phép: All, Unplayed, Played, Correct, Failed.");

            RuleFor(c => c.Request.CefrLevel)
                .Must(l => string.IsNullOrWhiteSpace(l) || AllowedCefrLevels.Contains(l.Trim()))
                .WithMessage("Trình độ CEFR không hợp lệ. Cho phép: null, A1, A2, B1, B2, C1, C2.");

            RuleFor(c => c.Request.TimeLimitSeconds)
                .InclusiveBetween(1, 120)
                .WithMessage("Thời gian giới hạn mỗi từ phải từ 1 đến 120 giây.");

            RuleFor(c => c.Request.RequestedCount)
                .InclusiveBetween(1, 100)
                .WithMessage("Số lượt chơi phải từ 1 đến 100 từ.");
        });
    }
}
