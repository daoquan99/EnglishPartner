using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;

public sealed class ListTopicsValidator : AbstractValidator<ListTopicsQuery>
{
    public ListTopicsValidator()
    {
        // Add specific validation rules here if needed
    }
}
