namespace EnglishPartner.Modules.Identity.Domain.Roles;

/// <summary>
/// All permission codes in the system.
/// Convention: "module.resource.action"
///
/// These codes are included as "permission" claims in Admin JWT tokens only.
/// Regular users (Learner) just need RequireAuthorization() — no permission claims required.
/// Admin endpoints use specific named policies based on these codes.
/// </summary>
public static class Permissions
{
    public static class Curriculum
    {
        public const string TopicsWrite       = "curriculum.topics.write";
        public const string VocabularyWrite   = "curriculum.vocabulary.write";
        public const string GrammarWrite      = "curriculum.grammar.write";
        public const string ExerciseWrite     = "curriculum.exercise.write";
        public const string ConversationWrite = "curriculum.conversation.write";
        public const string SentenceWrite     = "curriculum.sentence.write";
        public const string Generate          = "curriculum.generate";
    }

    public static class Learning
    {
        public const string AdminView  = "learning.admin.view";
        public const string AdminWrite = "learning.admin.write";
    }

    public static class Notifications
    {
        public const string Admin = "notifications.admin";
    }

    public static class Identity
    {
        public const string UsersManage = "identity.users.manage";
        public const string RolesManage = "identity.roles.manage";
    }

    public static class Ai
    {
        public const string LogsRead = "ai.logs.read";
    }

    public static class System
    {
        public const string Admin = "admin";
    }

    /// <summary>
    /// All permissions granted to the Admin role.
    /// These become "permission" claims in the Admin's JWT.
    /// </summary>
    public static readonly string[] AdminPermissions =
    [
        Curriculum.TopicsWrite,
        Curriculum.VocabularyWrite,
        Curriculum.GrammarWrite,
        Curriculum.ExerciseWrite,
        Curriculum.ConversationWrite,
        Curriculum.SentenceWrite,
        Curriculum.Generate,
        Learning.AdminView,
        Learning.AdminWrite,
        Identity.UsersManage,
        Identity.RolesManage,
        Ai.LogsRead,
        Notifications.Admin,
        System.Admin,
    ];
}