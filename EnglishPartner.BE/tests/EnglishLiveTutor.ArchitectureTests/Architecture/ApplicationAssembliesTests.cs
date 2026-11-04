using System.Xml.Linq;
using EnglishLiveTutor.Application;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed class ApplicationAssembliesTests
{
    [Fact]
    public void AllModuleAndSharedKernelProjects_AreRegistered()
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var modulesRoot = Path.Combine(root, "src", "Modules");
        var sharedKernelRoot = Path.Combine(root, "src", "Shared", "EnglishLiveTutor.Shared.Application");

        Assert.True(
            Directory.Exists(modulesRoot),
            $"Module source root was not found: {modulesRoot}");
        Assert.True(
            Directory.Exists(sharedKernelRoot),
            $"SharedKernel source root was not found: {sharedKernelRoot}");

        var projectFiles = Directory
            .EnumerateFiles(modulesRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                sharedKernelRoot,
                "*.csproj",
                SearchOption.AllDirectories))
            .Where(path => !HasExcludedSegment(path))
            .Where(IsMediatRAssemblyProject)
            .ToArray();
        Assert.NotEmpty(projectFiles);

        var registeredAssemblies = ApplicationAssemblies.All
            .Select(assembly => assembly.GetName().Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);
        var missing = projectFiles
            .Select(ReadAssemblyName)
            .Where(assemblyName => !registeredAssemblies.Contains(assemblyName))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Projects missing from ApplicationAssemblies.All:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, missing));
    }

    private static string ReadAssemblyName(string projectPath)
    {
        var project = XDocument.Load(projectPath);
        var assemblyName = project
            .Descendants("AssemblyName")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => value.Length > 0);

        return assemblyName ?? Path.GetFileNameWithoutExtension(projectPath);
    }

    private static bool HasExcludedSegment(string path)
    {
        return path
            .Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment is "bin" or "obj");
    }

    private static bool IsMediatRAssemblyProject(string projectPath)
    {
        string projectName = Path.GetFileNameWithoutExtension(projectPath);
        if (projectName == "EnglishLiveTutor.Shared.Application")
        {
            return true;
        }

        if (!projectName.StartsWith("EnglishLiveTutor.Modules.", StringComparison.Ordinal))
        {
            return false;
        }

        return projectName.EndsWith(".Application", StringComparison.Ordinal) ||
               projectName.EndsWith(".Contracts", StringComparison.Ordinal) ||
               projectName.EndsWith(".Domain", StringComparison.Ordinal) ||
               projectName.Count(character => character == '.') == 2;
    }
}
