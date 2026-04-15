using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Domain;

public static class LearningErrors
{
    public static class Profile
    {
        public static Error NotFound(Guid userId) =>
            new("Learning.Profile.NotFound", $"Learning profile for user '{userId}' was not found.");
    }

    public static class Session
    {
        public static Error NotFound(Guid id) =>
            new("Learning.Session.NotFound", $"Study session '{id}' was not found.");

        public static readonly Error NotInProgress =
            new("Learning.Session.NotInProgress", "Session is not in progress.");
    }
}
