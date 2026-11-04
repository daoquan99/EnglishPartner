using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed record Violation(string Kind, string SourcePath, string Target)
{
    public override string ToString() => $"{Kind} {SourcePath} -> {Target}";
}

public static class ModuleBoundaryScanner
{
    private static readonly HashSet<string> SkippedDirectories =
        new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Migrations" };

    private static readonly Regex UsingReferenceRegex = new(
        @"^\s*(?:global\s+)?using\s+(?:(?:static)\s+)?(?:(?<alias>[A-Za-z_]\w*)\s*=\s*)?(?<target>EnglishLiveTutor\.(?:Domain|Application\.(?:Features|Modules)|Infrastructure)\.[A-Za-z_][\w.]*)\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FullyQualifiedReferenceRegex = new(
        @"EnglishLiveTutor\.(?:Domain|Application\.(?:Features|Modules)|Infrastructure)\.[A-Za-z_][\w.]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex EntityFrameworkUsingRegex = new(
        @"^\s*(?:global\s+)?using\s+(?:(?:static)\s+)?Microsoft\.EntityFrameworkCore(?:\.[A-Za-z_][\w.]*)?\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DbContextTokenRegex = new(
        @"\bIAppDbContext(?:Factory)?\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HandlerTokenRegex = new(
        @"\b(?:IRequestHandler|ICommandHandler|IQueryHandler)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<Violation> Scan()
    {
        var root = FindSolutionRoot();
        var sourceRoot = Path.Combine(root, "src");
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source directory was not found: {sourceRoot}");
        }

        var violations = new HashSet<Violation>();
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (HasSkippedDirectory(sourceRoot, file))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceRoot, file).Replace('\\', '/');
            foreach (var violation in ScanFile(relativePath, File.ReadAllText(file)))
            {
                violations.Add(violation);
            }
        }

        return SortAndDeduplicate(violations);
    }

    public static IReadOnlyList<Violation> ScanFile(string relPath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relPath);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedPath = relPath.Replace('\\', '/').TrimStart('/');
        var sourceModule = ModuleMap.ModuleOfFile(normalizedPath);
        if (sourceModule is null)
        {
            return Array.Empty<Violation>();
        }

        var violations = new HashSet<Violation>();
        var references = ExtractReferences(content);
        foreach (var target in references)
        {
            var targetModule = ModuleMap.ModuleOfNamespace(target);
            if (targetModule is null ||
                targetModule == sourceModule ||
                targetModule == ModuleName.Shared)
            {
                continue;
            }

            if (!ModuleMap.AllowedDependencies[sourceModule.Value].Contains(targetModule.Value))
            {
                violations.Add(new Violation("dag", normalizedPath, target));
            }
            else if (!ContainsSegment(target, "Contracts"))
            {
                violations.Add(new Violation("internal", normalizedPath, target));
            }
        }

        if (IsApplicationFeatureFile(normalizedPath))
        {
            foreach (var line in NonCommentLines(content))
            {
                foreach (Match match in DbContextTokenRegex.Matches(line))
                {
                    violations.Add(new Violation("appdb", normalizedPath, match.Value));
                }
            }
        }

        if (IsContractsFile(normalizedPath))
        {
            foreach (var line in NonCommentLines(content))
            {
                var efUsing = EntityFrameworkUsingRegex.Match(line);
                if (efUsing.Success)
                {
                    violations.Add(
                        new Violation("contracts", normalizedPath, "Microsoft.EntityFrameworkCore"));
                }

                foreach (Match match in HandlerTokenRegex.Matches(line))
                {
                    violations.Add(new Violation("contracts", normalizedPath, match.Value));
                }
            }

            foreach (var target in references)
            {
                if (!target.StartsWith("EnglishLiveTutor.Domain.", StringComparison.Ordinal) ||
                    ContainsSegment(target, "Enums") ||
                    ContainsSegment(target, "Contracts"))
                {
                    continue;
                }

                var targetModule = ModuleMap.ModuleOfNamespace(target);
                if (targetModule != ModuleName.Shared)
                {
                    violations.Add(new Violation("contracts", normalizedPath, target));
                }
            }
        }

        return SortAndDeduplicate(violations);
    }

    public static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EnglishLiveTutor.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find EnglishLiveTutor.slnx above '{AppContext.BaseDirectory}'.");
    }

    private static IReadOnlySet<string> ExtractReferences(string content)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in NonCommentLines(content))
        {
            var usingMatch = UsingReferenceRegex.Match(line);
            if (usingMatch.Success)
            {
                references.Add(usingMatch.Groups["target"].Value);
            }

            foreach (Match match in FullyQualifiedReferenceRegex.Matches(line))
            {
                references.Add(match.Value);
            }
        }

        return references;
    }

    private static IEnumerable<string> NonCommentLines(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal));
    }

    private static bool ContainsSegment(string value, string segment)
    {
        return value.Split('.').Contains(segment, StringComparer.Ordinal);
    }

    private static bool IsApplicationFeatureFile(string relPath)
    {
        return relPath.Contains(".Application/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractsFile(string relPath)
    {
        return relPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.Equals("Contracts", StringComparison.Ordinal)
                            || segment.EndsWith(".Contracts", StringComparison.Ordinal));
    }

    private static bool HasSkippedDirectory(string sourceRoot, string file)
    {
        var relativeDirectory = Path.GetRelativePath(sourceRoot, Path.GetDirectoryName(file)!);
        return relativeDirectory
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(SkippedDirectories.Contains);
    }

    private static IReadOnlyList<Violation> SortAndDeduplicate(IEnumerable<Violation> violations)
    {
        return violations
            .Distinct()
            .OrderBy(violation => violation.Kind, StringComparer.Ordinal)
            .ThenBy(violation => violation.SourcePath, StringComparer.Ordinal)
            .ThenBy(violation => violation.Target, StringComparer.Ordinal)
            .ToArray();
    }
}
