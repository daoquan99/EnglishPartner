using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Roles.Entities;

public sealed class Permission : Entity
{
    private Permission() { }

    public static Permission Create(string code, string name, string module, string? description = null)
    {
        return new Permission { Id = Guid.NewGuid(), Code = code, Name = name, Module = module, Description = description };
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Module { get; private set; } = default!;
    public string? Description { get; private set; }
}
