using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Identity.Permissions;

public sealed class PermissionEntity : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty; // e.g. users.manage, vocabularies.generate
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = "General";
    public string Description { get; private set; } = string.Empty;

    private PermissionEntity()
    {
    }

    public PermissionEntity(
        string code,
        string name,
        string module = "General",
        string description = "",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Mã quyền (Permission Code) không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên quyền không được để trống.");

        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        Module = string.IsNullOrWhiteSpace(module) ? "General" : module.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public void UpdateDetails(string name, string module, string description)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(module))
            Module = module.Trim();
        Description = description?.Trim() ?? string.Empty;
    }
}
