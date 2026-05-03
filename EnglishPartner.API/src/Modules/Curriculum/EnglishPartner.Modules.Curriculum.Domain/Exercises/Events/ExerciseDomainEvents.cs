using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Exercises.Events;

public sealed record ExerciseCreatedDomainEvent(Guid ExerciseId) : DomainEvent;
