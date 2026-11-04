using System.Collections.ObjectModel;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public enum ModuleName
{
    Identity,
    Catalog,
    Speech,
    Lessons,
    Grammar,
    Vocabulary,
    LiveTutor,
    Progress,
    Shared
}

public static class ModuleMap
{
    private static readonly IReadOnlyDictionary<string, ModuleName> DomainSegments =
        new Dictionary<string, ModuleName>(StringComparer.Ordinal)
        {
            ["Identity"] = ModuleName.Identity,
            ["AiCatalog"] = ModuleName.Catalog,
            ["Prompts"] = ModuleName.Catalog,
            ["Voices"] = ModuleName.Catalog,
            ["Topics"] = ModuleName.Catalog,
            ["Pronunciation"] = ModuleName.Speech,
            ["Lessons"] = ModuleName.Lessons,
            ["LessonAttempts"] = ModuleName.Lessons,
            ["Grammar"] = ModuleName.Grammar,
            ["Vocabulary"] = ModuleName.Vocabulary,
            ["Learning"] = ModuleName.Vocabulary,
            ["VocabularyGames"] = ModuleName.Vocabulary,
            ["Conversations"] = ModuleName.LiveTutor,
            ["Progress"] = ModuleName.Progress,
            ["Common"] = ModuleName.Shared
        };

    private static readonly IReadOnlyDictionary<string, ModuleName> FeatureSegments =
        new Dictionary<string, ModuleName>(StringComparer.Ordinal)
        {
            ["Identity"] = ModuleName.Identity,
            ["AiModels"] = ModuleName.Catalog,
            ["Prompts"] = ModuleName.Catalog,
            ["Voices"] = ModuleName.Catalog,
            ["Topics"] = ModuleName.Catalog,
            ["Pronunciation"] = ModuleName.Speech,
            ["Lessons"] = ModuleName.Lessons,
            ["Grammar"] = ModuleName.Grammar,
            ["Vocabulary"] = ModuleName.Vocabulary,
            ["Learning"] = ModuleName.Vocabulary,
            ["VocabularyGames"] = ModuleName.Vocabulary,
            ["Conversations"] = ModuleName.LiveTutor,
            ["Progress"] = ModuleName.Progress
        };

    private static readonly IReadOnlyDictionary<string, ModuleName> ModuleSegments =
        new Dictionary<string, ModuleName>(StringComparer.Ordinal)
        {
            ["Identity"] = ModuleName.Identity,
            ["Catalog"] = ModuleName.Catalog,
            ["Speech"] = ModuleName.Speech,
            ["Lessons"] = ModuleName.Lessons,
            ["Grammar"] = ModuleName.Grammar,
            ["Vocabulary"] = ModuleName.Vocabulary,
            ["LiveTutor"] = ModuleName.LiveTutor,
            ["Progress"] = ModuleName.Progress
        };

    private static readonly IReadOnlyDictionary<string, ModuleName> InfrastructureSegments =
        new Dictionary<string, ModuleName>(StringComparer.Ordinal)
        {
            ["Identity"] = ModuleName.Identity,
            ["AiCatalog"] = ModuleName.Catalog,
            ["Prompts"] = ModuleName.Catalog,
            ["Voices"] = ModuleName.Catalog,
            ["Topics"] = ModuleName.Catalog,
            ["Pronunciation"] = ModuleName.Speech,
            ["Lessons"] = ModuleName.Lessons,
            ["Storage"] = ModuleName.Lessons,
            ["Grammar"] = ModuleName.Grammar,
            ["Vocabulary"] = ModuleName.Vocabulary,
            ["VocabularyGames"] = ModuleName.Vocabulary,
            ["Realtime"] = ModuleName.LiveTutor
        };

    private static readonly IReadOnlyDictionary<ModuleName, IReadOnlySet<ModuleName>> Dependencies =
        CreateDependencies();

    private static readonly IReadOnlySet<string> SharedKernelFileRules =
        CreateSharedKernelFileRules();

    public static IReadOnlyDictionary<ModuleName, IReadOnlySet<ModuleName>> AllowedDependencies =>
        Dependencies;

    public static IReadOnlySet<string> SharedKernelFiles => SharedKernelFileRules;
    public static IReadOnlySet<string> HostFiles => HostFileRules;
    private static readonly IReadOnlySet<string> HostFileRules = CreateHostFileRules();

    public static ModuleName? ModuleOfNamespace(string ns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);

        const string modulesNamespace = "EnglishLiveTutor.Application.Modules";
        if (ns.Equals(modulesNamespace, StringComparison.Ordinal))
        {
            return ModuleName.Shared;
        }

        const string modulePrefix = modulesNamespace + ".";
        if (ns.StartsWith(modulePrefix, StringComparison.Ordinal))
        {
            return ResolveSegment(ns, modulePrefix, ModuleSegments);
        }

        const string domainPrefix = "EnglishLiveTutor.Domain.";
        if (ns.StartsWith(domainPrefix, StringComparison.Ordinal))
        {
            return ResolveSegment(ns, domainPrefix, DomainSegments);
        }

        const string featurePrefix = "EnglishLiveTutor.Application.Features.";
        if (ns.StartsWith(featurePrefix, StringComparison.Ordinal))
        {
            return ResolveSegment(ns, featurePrefix, FeatureSegments);
        }

        const string infrastructureNamespace = "EnglishLiveTutor.Infrastructure";
        if (ns.Equals(infrastructureNamespace, StringComparison.Ordinal))
        {
            return ModuleName.Shared;
        }

        const string infrastructurePrefix = infrastructureNamespace + ".";
        if (ns.StartsWith(infrastructurePrefix, StringComparison.Ordinal))
        {
            var remainder = ns[infrastructurePrefix.Length..];
            var separator = remainder.IndexOf('.');
            var segment = separator >= 0 ? remainder[..separator] : remainder;
            return InfrastructureSegments.TryGetValue(segment, out var module)
                ? module
                : null;
        }

        return null;
    }

    public static ModuleName? ModuleOfFile(string relPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relPath);
        var path = relPath.Replace('\\', '/').TrimStart('/');

        const string modularModulesRoot = "Modules/";
        if (TryGetChildSegment(path, modularModulesRoot, out var moduleSegment))
        {
            return ModuleSegments.TryGetValue(moduleSegment, out var module)
                ? module
                : null;
        }

        const string sharedKernelRoot = "Shared/EnglishLiveTutor.Shared.Application/";
        if (path.Equals(
                sharedKernelRoot.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(sharedKernelRoot, StringComparison.OrdinalIgnoreCase))
        {
            return ModuleName.Shared;
        }

        const string sharedInfrastructureRoot = "Shared/EnglishLiveTutor.Shared.Infrastructure/";
        if (path.Equals(
                sharedInfrastructureRoot.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(sharedInfrastructureRoot, StringComparison.OrdinalIgnoreCase))
        {
            return ModuleName.Shared;
        }

        const string sharedPresentationRoot = "Shared/EnglishLiveTutor.Shared.Presentation/";
        if (path.Equals(
                sharedPresentationRoot.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(sharedPresentationRoot, StringComparison.OrdinalIgnoreCase))
        {
            return ModuleName.Shared;
        }

        return null;
    }

    public static bool IsSharedKernelFile(string relPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relPath);
        var path = relPath.Replace('\\', '/').TrimStart('/');

        return SharedKernelFiles.Any(rule =>
            rule.EndsWith("/", StringComparison.Ordinal)
                ? path.StartsWith(rule, StringComparison.OrdinalIgnoreCase)
                : path.Equals(rule, StringComparison.OrdinalIgnoreCase));
    }

    private static ModuleName ResolveSegment(
        string ns,
        string prefix,
        IReadOnlyDictionary<string, ModuleName> segments)
    {
        var remainder = ns[prefix.Length..];
        var separator = remainder.IndexOf('.');
        var segment = separator >= 0 ? remainder[..separator] : remainder;

        if (segments.TryGetValue(segment, out var module))
        {
            return module;
        }

        throw new InvalidOperationException(
            $"Unmapped module namespace segment '{segment}' in '{ns}'.");
    }

    private static bool TryGetChildSegment(string path, string prefix, out string segment)
    {
        segment = string.Empty;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = path[prefix.Length..];
        var separator = remainder.IndexOf('/');
        if (separator <= 0)
        {
            return false;
        }

        segment = remainder[..separator];
        return true;
    }

    private static IReadOnlyDictionary<ModuleName, IReadOnlySet<ModuleName>> CreateDependencies()
    {
        var dependencies = new Dictionary<ModuleName, IReadOnlySet<ModuleName>>
        {
            [ModuleName.Identity] = ReadOnlySet(),
            [ModuleName.Catalog] = ReadOnlySet(ModuleName.Identity),
            [ModuleName.Speech] = ReadOnlySet(ModuleName.Identity, ModuleName.Catalog),
            [ModuleName.Lessons] = ReadOnlySet(ModuleName.Identity, ModuleName.Speech, ModuleName.Catalog),
            [ModuleName.Grammar] = ReadOnlySet(ModuleName.Identity, ModuleName.Catalog, ModuleName.Lessons, ModuleName.Speech),
            [ModuleName.Vocabulary] = ReadOnlySet(ModuleName.Identity, ModuleName.Speech, ModuleName.Catalog),
            [ModuleName.LiveTutor] = ReadOnlySet(ModuleName.Identity, ModuleName.Catalog),
            [ModuleName.Progress] = ReadOnlySet(ModuleName.Identity, ModuleName.Vocabulary, ModuleName.Lessons, ModuleName.Grammar, ModuleName.Speech, ModuleName.LiveTutor),
            [ModuleName.Shared] = ReadOnlySet()
        };

        return new ReadOnlyDictionary<ModuleName, IReadOnlySet<ModuleName>>(dependencies);
    }

    private static IReadOnlySet<string> CreateSharedKernelFileRules()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Shared/EnglishLiveTutor.Shared.Application/",
            "EnglishLiveTutor.Api/Composition/",
            "EnglishLiveTutor.Api/Program.cs",
            "EnglishLiveTutor.Api/Common/",
            "EnglishLiveTutor.Api/Configuration/",
            "EnglishLiveTutor.Api/Exceptions/",
            "EnglishLiveTutor.Api/Security/",
            "EnglishLiveTutor.Api/Services/HttpContextCurrentUser.cs",
            "EnglishLiveTutor.Api/Services/ClaimsPermissionChecker.cs",

            "EnglishLiveTutor.Api/Endpoints/HealthEndpoints.cs"
        };
    }

    private static IReadOnlySet<string> CreateHostFileRules()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EnglishLiveTutor.Api/Services/LiveFeedbackAudioSynthesizer.cs"
        };
    }

    public static bool IsHostFile(string relPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relPath);
        var path = relPath.Replace('\\', '/').TrimStart('/');

        return HostFiles.Any(rule =>
            rule.EndsWith("/", StringComparison.Ordinal)
                ? path.StartsWith(rule, StringComparison.OrdinalIgnoreCase)
                : path.Equals(rule, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlySet<ModuleName> ReadOnlySet(params ModuleName[] modules)
    {
        return new HashSet<ModuleName>(modules);
    }
}
