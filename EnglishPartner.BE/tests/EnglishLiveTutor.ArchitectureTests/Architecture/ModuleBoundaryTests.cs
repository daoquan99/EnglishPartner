namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    private const string AllowlistHeader =
        "# Module-boundary baseline for ADR-0006: only remove entries; never add new entries.";

    [Fact]
    public void NoNewModuleBoundaryViolations()
    {
        var violations = ModuleBoundaryScanner.Scan();
        var violationLines = violations.Select(violation => violation.ToString()).ToArray();
        var allowlistPath = GetAllowlistPath();
        var writeAllowlist = Environment.GetEnvironmentVariable("MODULE_ALLOWLIST_WRITE") == "1";

        if (writeAllowlist && !File.Exists(allowlistPath))
        {
            WriteAllowlist(allowlistPath, violationLines);
            return;
        }

        var allowed = ReadAllowlist(allowlistPath);
        if (writeAllowlist)
        {
            WriteAllowlist(
                allowlistPath,
                violationLines.Where(allowed.Contains).ToArray());
        }

        var newViolations = violationLines
            .Where(violation => !allowed.Contains(violation))
            .ToArray();

        Assert.True(
            newViolations.Length == 0,
            "New module-boundary violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, newViolations));
    }

    [Fact]
    public void AllowlistStartsWithComment()
    {
        var firstLine = File.ReadLines(GetAllowlistPath()).FirstOrDefault();

        Assert.NotNull(firstLine);
        Assert.StartsWith("#", firstLine);
    }

    [Fact]
    public void AllowlistHasNoStaleEntries()
    {
        var current = ModuleBoundaryScanner.Scan()
            .Select(violation => violation.ToString())
            .ToHashSet(StringComparer.Ordinal);
        var stale = ReadAllowlist(GetAllowlistPath())
            .Where(violation => !current.Contains(violation))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            stale.Length == 0,
            "Stale module-boundary allowlist entries:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void EveryDomainAndFeatureFolderIsMapped()
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var sourceRoot = Path.Combine(root, "src");
        var modulesRoot = Path.Combine(sourceRoot, "Modules");
        var domainRoot = Path.Combine(root, "src", "Shared", "EnglishLiveTutor.Shared.Domain");

        Assert.True(
            Directory.Exists(modulesRoot),
            $"Module source root was not found: {modulesRoot}");
        Assert.True(
            Directory.Exists(domainRoot),
            $"Domain source root was not found: {domainRoot}");

        var moduleProjects = Directory
            .EnumerateDirectories(modulesRoot)
            .SelectMany(Directory.EnumerateDirectories);
        var featureFolders = moduleProjects
            .Select(project => Path.Combine(project, "Features"))
            .Where(Directory.Exists)
            .SelectMany(path => EnumerateMappedFolders(
                path,
                "EnglishLiveTutor.Application.Features"));
        var moduleDomainFolders = moduleProjects
            .Select(project => Path.Combine(project, "Domain"))
            .Where(Directory.Exists)
            .SelectMany(path => EnumerateMappedFolders(
                path,
                "EnglishLiveTutor.Domain"));
        var folders = EnumerateMappedFolders(domainRoot, "EnglishLiveTutor.Domain")
            .Concat(featureFolders)
            .Concat(moduleDomainFolders)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(folders);

        var unmapped = new List<string>();
        foreach (var ns in folders)
        {
            try
            {
                _ = ModuleMap.ModuleOfNamespace(ns);
            }
            catch (InvalidOperationException exception)
            {
                unmapped.Add(exception.Message);
            }
        }

        Assert.True(
            unmapped.Count == 0,
            "Unmapped Domain/Features folders:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, unmapped.Order(StringComparer.Ordinal)));
        Assert.Null(ModuleMap.ModuleOfFile("Shared/EnglishLiveTutor.Shared.Domain/Common/Entity.cs"));
    }

    private static IEnumerable<string> EnumerateMappedFolders(string path, string namespacePrefix)
    {
        return Directory
            .EnumerateDirectories(path)
            .Select(Path.GetFileName)
            .Where(folder => folder is not null && folder is not "bin" and not "obj")
            .Select(folder => $"{namespacePrefix}.{folder}");
    }

    [Fact]
    public void ModuleProjectPath_IsMappedToOwningModule()
    {
        Assert.Equal(
            ModuleName.Lessons,
            ModuleMap.ModuleOfFile(
                "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Domain/Domain/Lessons/LessonEntity.cs"));
        Assert.Equal(
            ModuleName.Shared,
            ModuleMap.ModuleOfFile(
                "Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/ModuleSchemas.cs"));
        Assert.Equal(
            ModuleName.Shared,
            ModuleMap.ModuleOfFile(
                "Shared/EnglishLiveTutor.Shared.Presentation/Endpoints/PaginationEndpointExtensions.cs"));
    }

    [Fact]
    public void AllowedDependenciesAreAcyclic()
    {
        var visiting = new HashSet<ModuleName>();
        var visited = new HashSet<ModuleName>();

        void Visit(ModuleName module)
        {
            if (visited.Contains(module))
            {
                return;
            }

            Assert.True(
                visiting.Add(module),
                $"Cycle detected in AllowedDependencies at module '{module}'.");

            foreach (var dependency in ModuleMap.AllowedDependencies[module])
            {
                Visit(dependency);
            }

            visiting.Remove(module);
            visited.Add(module);
        }

        foreach (var module in ModuleMap.AllowedDependencies.Keys)
        {
            Visit(module);
        }
    }

    [Fact]
    public void ScanFile_DisallowedModuleReference_ReturnsDagViolation()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Identity/EnglishLiveTutor.Modules.Identity.Application/Features/Handler.cs",
            "using EnglishLiveTutor.Domain.Lessons.Entities;");

        Assert.Contains(
            new Violation(
                "dag",
                "Modules/Identity/EnglishLiveTutor.Modules.Identity.Application/Features/Handler.cs",
                "EnglishLiveTutor.Domain.Lessons.Entities"),
            violations);
    }

    [Fact]
    public void ScanFile_AllowedModuleWithoutContracts_ReturnsInternalViolation()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
            "using EnglishLiveTutor.Domain.Lessons.Entities;");

        Assert.Contains(
            new Violation(
                "internal",
                "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
                "EnglishLiveTutor.Domain.Lessons.Entities"),
            violations);
    }

    [Fact]
    public void ScanFile_UnambiguousInfrastructureNamespaceWithoutContracts_ReturnsInternalViolation()
    {
        var path = "Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Infrastructure/Ai/CefrPromptBuilder.cs";
        var violations = ModuleBoundaryScanner.ScanFile(
            path,
            "using EnglishLiveTutor.Infrastructure.Prompts;");

        Assert.Contains(
            new Violation(
                "internal",
                path,
                "EnglishLiveTutor.Infrastructure.Prompts"),
            violations);
    }

    [Theory]
    [InlineData("EnglishLiveTutor.Infrastructure.Ai.GeminiHttpHelper")]
    [InlineData("EnglishLiveTutor.Infrastructure.Repositories")]
    [InlineData("EnglishLiveTutor.Infrastructure.Persistence")]
    public void ScanFile_MixedInfrastructureNamespace_IsIgnored(string target)
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Infrastructure/Ai/CefrPromptBuilder.cs",
            $"using {target};");

        Assert.Empty(violations);
        Assert.Null(ModuleMap.ModuleOfNamespace(target));
    }

    [Fact]
    public void ScanFile_ApplicationFeatureUsesSharedDbContext_ReturnsAppDbViolation()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Application/Features/Handler.cs",
            "private readonly IAppDbContext _db; private readonly IAppDbContextFactory _factory;");

        Assert.Contains(
            new Violation(
                "appdb",
                "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Application/Features/Handler.cs",
                "IAppDbContext"),
            violations);
        Assert.Contains(
            new Violation(
                "appdb",
                "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Application/Features/Handler.cs",
                "IAppDbContextFactory"),
            violations);
    }

    [Fact]
    public void ScanFile_ContractsUsesEf_ReturnsContractsViolation()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Speech/EnglishLiveTutor.Modules.Speech.Contracts/Features/ISpeechReader.cs",
            "using Microsoft.EntityFrameworkCore;");

        Assert.Contains(
            new Violation(
                "contracts",
                "Modules/Speech/EnglishLiveTutor.Modules.Speech.Contracts/Features/ISpeechReader.cs",
                "Microsoft.EntityFrameworkCore"),
            violations);
    }

    [Fact]
    public void ScanFile_DependencyThroughContracts_IsAllowed()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
            "using EnglishLiveTutor.Application.Features.Lessons.Contracts;");

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(
        "global using EnglishLiveTutor.Domain.Lessons.Entities;",
        "EnglishLiveTutor.Domain.Lessons.Entities")]
    [InlineData(
        "using static EnglishLiveTutor.Domain.Lessons.Entities;",
        "EnglishLiveTutor.Domain.Lessons.Entities")]
    [InlineData(
        "using Lessons = EnglishLiveTutor.Domain.Lessons.Entities;",
        "EnglishLiveTutor.Domain.Lessons.Entities")]
    [InlineData(
        "var entity = new EnglishLiveTutor.Domain.Lessons.Entities.LessonEntity();",
        "EnglishLiveTutor.Domain.Lessons.Entities.LessonEntity")]
    public void ScanFile_RecognizedReferenceSyntax_ReturnsInternalViolation(
        string content,
        string expectedTarget)
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
            content);

        Assert.Contains(
            new Violation(
                "internal",
                "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
                expectedTarget),
            violations);
    }

    [Fact]
    public void ScanFile_CommentLineReference_IsIgnored()
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs",
            "// using EnglishLiveTutor.Domain.Lessons.Entities;");

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("IRequestHandler")]
    [InlineData("ICommandHandler")]
    [InlineData("IQueryHandler")]
    public void ScanFile_ContractsContainsHandlerToken_ReturnsContractsViolation(string token)
    {
        var path = "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Contracts/Features/IHandler.cs";
        var violations = ModuleBoundaryScanner.ScanFile(path, token);

        Assert.Contains(new Violation("contracts", path, token), violations);
    }

    [Fact]
    public void ScanFile_ContractsReferencesOwnModuleEntity_ReturnsContractsViolation()
    {
        var path = "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Contracts/Features/ILessonReader.cs";
        var violations = ModuleBoundaryScanner.ScanFile(
            path,
            "using EnglishLiveTutor.Domain.Lessons.Entities;");

        Assert.Contains(
            new Violation(
                "contracts",
                path,
                "EnglishLiveTutor.Domain.Lessons.Entities"),
            violations);
    }

    [Theory]
    [InlineData("EnglishLiveTutor.Domain.Common.Primitives")]
    [InlineData("EnglishLiveTutor.Domain.Common.Exercises")]
    public void ScanFile_ContractsReferencesAllowedDomainNamespace_ReturnsNoViolation(
        string target)
    {
        var violations = ModuleBoundaryScanner.ScanFile(
            "Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Contracts/Features/ILessonReader.cs",
            $"using {target};");

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(
        "Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Infrastructure/Ai/CefrPromptBuilder.cs",
        ModuleName.LiveTutor)]
    [InlineData(
        "Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/SharedDbContext.cs",
        ModuleName.Shared)]
    [InlineData(
        "Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Presentation/Hubs/TutorConversationHub.cs",
        ModuleName.LiveTutor)]
    public void ModuleOfFile_ClassifiesInfrastructureAndApiEvidence(
        string path,
        ModuleName? expected)
    {
        Assert.Equal(expected, ModuleMap.ModuleOfFile(path));
    }

    [Fact]
    public void EveryInfrastructureAndApiFileIsClassified()
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var sourceRoot = Path.Combine(root, "src");
        var unclassified = new[]
            {
                "Shared/EnglishLiveTutor.Shared.Infrastructure",
                "Shared/EnglishLiveTutor.Shared.Presentation",
                "EnglishLiveTutor.Infrastructure",
                "EnglishLiveTutor.Api"
            }
            .Select(project => Path.Combine(sourceRoot, project))
            .Where(Directory.Exists)
            .SelectMany(projectRoot => Directory.EnumerateFiles(
                projectRoot,
                "*.cs",
                SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(sourceRoot, file).Replace('\\', '/'))
            .Where(path => !HasExcludedSegment(path))
            .Where(path =>
                ModuleMap.ModuleOfFile(path) is null &&
                !ModuleMap.IsSharedKernelFile(path) && !ModuleMap.IsHostFile(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unclassified.Length == 0,
            "Unclassified Shared/Infrastructure/Api files:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, unclassified));
    }

    private static HashSet<string> ReadAllowlist(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Module-boundary allowlist was not found.", path);
        }

        return File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string GetAllowlistPath()
    {
        return Path.Combine(
            ModuleBoundaryScanner.FindSolutionRoot(),
            "tests",
            "EnglishLiveTutor.ArchitectureTests",
            "Architecture",
            "module-boundary-allowlist.txt");
    }

    private static void WriteAllowlist(string path, IEnumerable<string> entries)
    {
        File.WriteAllLines(
            path,
            new[] { AllowlistHeader }.Concat(entries),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool HasExcludedSegment(string path)
    {
        return path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment =>
                segment is "bin" or "obj" or "Migrations" or "Properties");
    }
}
