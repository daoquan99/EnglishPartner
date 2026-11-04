using System.Xml.Linq;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Domain_ShouldNotReferenceAnyProject()
    {
        Assert.Empty(ReadProjectReferences("EnglishLiveTutor.Shared.Domain"));
    }

    [Fact]
    public void SharedKernel_ShouldReferenceOnlyDomain()
    {
        var references = ReadProjectReferences("EnglishLiveTutor.Shared.Application");

        var reference = Assert.Single(references);
        Assert.Equal("EnglishLiveTutor.Shared.Domain.csproj", Path.GetFileName(reference));
    }

    [Fact]
    public void CatalogContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Catalog",
            "EnglishLiveTutor.Modules.Catalog.Contracts",
            "EnglishLiveTutor.Modules.Catalog.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CatalogDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Catalog",
            "EnglishLiveTutor.Modules.Catalog.Domain",
            "EnglishLiveTutor.Modules.Catalog.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CatalogApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Catalog",
            "EnglishLiveTutor.Modules.Catalog.Application",
            "EnglishLiveTutor.Modules.Catalog.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Catalog.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CatalogInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Catalog",
            "EnglishLiveTutor.Modules.Catalog.Infrastructure",
            "EnglishLiveTutor.Modules.Catalog.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Application.csproj",
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Catalog.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CatalogPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Catalog",
            "EnglishLiveTutor.Modules.Catalog.Presentation",
            "EnglishLiveTutor.Modules.Catalog.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Application.csproj",
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IdentityContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Identity",
            "EnglishLiveTutor.Modules.Identity.Contracts",
            "EnglishLiveTutor.Modules.Identity.Contracts.csproj");
        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IdentityDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Identity",
            "EnglishLiveTutor.Modules.Identity.Domain",
            "EnglishLiveTutor.Modules.Identity.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IdentityApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Identity",
            "EnglishLiveTutor.Modules.Identity.Application",
            "EnglishLiveTutor.Modules.Identity.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Modules.Identity.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IdentityInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Identity",
            "EnglishLiveTutor.Modules.Identity.Infrastructure",
            "EnglishLiveTutor.Modules.Identity.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Identity.Application.csproj",
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Modules.Identity.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IdentityPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Identity",
            "EnglishLiveTutor.Modules.Identity.Presentation",
            "EnglishLiveTutor.Modules.Identity.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Identity.Application.csproj",
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SpeechContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Speech",
            "EnglishLiveTutor.Modules.Speech.Contracts",
            "EnglishLiveTutor.Modules.Speech.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SpeechDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Speech",
            "EnglishLiveTutor.Modules.Speech.Domain",
            "EnglishLiveTutor.Modules.Speech.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SpeechApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Speech",
            "EnglishLiveTutor.Modules.Speech.Application",
            "EnglishLiveTutor.Modules.Speech.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SpeechInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Speech",
            "EnglishLiveTutor.Modules.Speech.Infrastructure",
            "EnglishLiveTutor.Modules.Speech.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Application.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SpeechPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Speech",
            "EnglishLiveTutor.Modules.Speech.Presentation",
            "EnglishLiveTutor.Modules.Speech.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Speech.Application.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void VocabularyContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Vocabulary",
            "EnglishLiveTutor.Modules.Vocabulary.Contracts",
            "EnglishLiveTutor.Modules.Vocabulary.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void VocabularyDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Vocabulary",
            "EnglishLiveTutor.Modules.Vocabulary.Domain",
            "EnglishLiveTutor.Modules.Vocabulary.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void VocabularyApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Vocabulary",
            "EnglishLiveTutor.Modules.Vocabulary.Application",
            "EnglishLiveTutor.Modules.Vocabulary.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void VocabularyInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Vocabulary",
            "EnglishLiveTutor.Modules.Vocabulary.Infrastructure",
            "EnglishLiveTutor.Modules.Vocabulary.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Application.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void VocabularyPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Vocabulary",
            "EnglishLiveTutor.Modules.Vocabulary.Presentation",
            "EnglishLiveTutor.Modules.Vocabulary.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Vocabulary.Application.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LessonsContracts_ShouldReferenceOnlyAllowedContracts()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Lessons",
            "EnglishLiveTutor.Modules.Lessons.Contracts",
            "EnglishLiveTutor.Modules.Lessons.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LessonsDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Lessons",
            "EnglishLiveTutor.Modules.Lessons.Domain",
            "EnglishLiveTutor.Modules.Lessons.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LessonsApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Lessons",
            "EnglishLiveTutor.Modules.Lessons.Application",
            "EnglishLiveTutor.Modules.Lessons.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Domain.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LessonsInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Lessons",
            "EnglishLiveTutor.Modules.Lessons.Infrastructure",
            "EnglishLiveTutor.Modules.Lessons.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Application.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Domain.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LessonsPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Lessons",
            "EnglishLiveTutor.Modules.Lessons.Presentation",
            "EnglishLiveTutor.Modules.Lessons.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Lessons.Application.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GrammarContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Grammar",
            "EnglishLiveTutor.Modules.Grammar.Contracts",
            "EnglishLiveTutor.Modules.Grammar.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GrammarDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Grammar",
            "EnglishLiveTutor.Modules.Grammar.Domain",
            "EnglishLiveTutor.Modules.Grammar.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GrammarApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Grammar",
            "EnglishLiveTutor.Modules.Grammar.Application",
            "EnglishLiveTutor.Modules.Grammar.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Grammar.Contracts.csproj",
                "EnglishLiveTutor.Modules.Grammar.Domain.csproj",
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GrammarInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Grammar",
            "EnglishLiveTutor.Modules.Grammar.Infrastructure",
            "EnglishLiveTutor.Modules.Grammar.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.Grammar.Application.csproj",
                "EnglishLiveTutor.Modules.Grammar.Contracts.csproj",
                "EnglishLiveTutor.Modules.Grammar.Domain.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GrammarPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Grammar",
            "EnglishLiveTutor.Modules.Grammar.Presentation",
            "EnglishLiveTutor.Modules.Grammar.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Grammar.Application.csproj",
                "EnglishLiveTutor.Modules.Grammar.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LiveTutorContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "LiveTutor",
            "EnglishLiveTutor.Modules.LiveTutor.Contracts",
            "EnglishLiveTutor.Modules.LiveTutor.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LiveTutorDomain_ShouldReferenceOnlyDomainKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "LiveTutor",
            "EnglishLiveTutor.Modules.LiveTutor.Domain",
            "EnglishLiveTutor.Modules.LiveTutor.Domain.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LiveTutorApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "LiveTutor",
            "EnglishLiveTutor.Modules.LiveTutor.Application",
            "EnglishLiveTutor.Modules.LiveTutor.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Contracts.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LiveTutorInfrastructure_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "LiveTutor",
            "EnglishLiveTutor.Modules.LiveTutor.Infrastructure",
            "EnglishLiveTutor.Modules.LiveTutor.Infrastructure.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Catalog.Contracts.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Application.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Contracts.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Domain.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Infrastructure.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LiveTutorPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "LiveTutor",
            "EnglishLiveTutor.Modules.LiveTutor.Presentation",
            "EnglishLiveTutor.Modules.LiveTutor.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.LiveTutor.Application.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ProgressContracts_ShouldReferenceOnlyDomainAndSharedKernel()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Progress",
            "EnglishLiveTutor.Modules.Progress.Contracts",
            "EnglishLiveTutor.Modules.Progress.Contracts.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ProgressApplication_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Progress",
            "EnglishLiveTutor.Modules.Progress.Application",
            "EnglishLiveTutor.Modules.Progress.Application.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Grammar.Contracts.csproj",
                "EnglishLiveTutor.Modules.Identity.Contracts.csproj",
                "EnglishLiveTutor.Modules.Lessons.Contracts.csproj",
                "EnglishLiveTutor.Modules.LiveTutor.Contracts.csproj",
                "EnglishLiveTutor.Modules.Progress.Contracts.csproj",
                "EnglishLiveTutor.Modules.Speech.Contracts.csproj",
                "EnglishLiveTutor.Modules.Vocabulary.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Domain.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ProgressPresentation_ShouldReferenceOnlyAllowedProjects()
    {
        var references = ReadProjectReferences(
            "src",
            "Modules",
            "Progress",
            "EnglishLiveTutor.Modules.Progress.Presentation",
            "EnglishLiveTutor.Modules.Progress.Presentation.csproj");

        Assert.Equal(
            [
                "EnglishLiveTutor.Modules.Progress.Application.csproj",
                "EnglishLiveTutor.Modules.Progress.Contracts.csproj",
                "EnglishLiveTutor.Shared.Application.csproj",
                "EnglishLiveTutor.Shared.Presentation.csproj"
            ],
            references
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryModuleProject_ShouldRespectModuleDag()
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var sourceRoot = Path.Combine(root, "src");
        var modulesRoot = Path.Combine(sourceRoot, "Modules");

        Assert.True(
            Directory.Exists(modulesRoot),
            $"Module source root was not found: {modulesRoot}");

        var projectFiles = Directory
            .EnumerateFiles(modulesRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !HasExcludedSegment(path))
            .ToArray();
        // ADR-0006: 7 full modules (5 projects each = 35) + Progress module (3 projects: Contracts, Application, Presentation = 38).
        Assert.Equal(38, projectFiles.Length);

        var violations = new List<string>();
        foreach (var projectPath in projectFiles)
        {
            var relativePath = Path
                .GetRelativePath(sourceRoot, projectPath)
                .Replace('\\', '/');
            var module = ModuleMap.ModuleOfFile(relativePath);
            Assert.True(
                module is not null && module != ModuleName.Shared,
                $"Could not infer a module for project '{relativePath}'.");

            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var layer = GetProjectLayer(projectName, module.Value);
            var allowedProjects = GetAllowedProjects(module.Value, layer);

            foreach (var reference in ReadProjectReferencesFromFile(projectPath))
            {
                var referencedProject = GetProjectName(reference);
                if (!allowedProjects.Contains(referencedProject))
                {
                    violations.Add(
                        $"{relativePath} -> {referencedProject} " +
                        $"(module {module}, project kind " +
                        $"{layer})");
                }
            }

            if (layer == ModuleProjectLayer.Domain &&
                ReadPackageReferencesFromFile(projectPath).Count != 0)
            {
                violations.Add(
                    $"{relativePath} -> PackageReference " +
                    "(module Domain projects may reference the BCL only)");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Project/package references outside the module layer rules:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations.Order(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("EnglishLiveTutor.Shared.Infrastructure")]
    [InlineData("EnglishLiveTutor.Shared.Presentation")]
    public void SharedProject_ShouldNotReferenceModuleProjects(string projectName)
    {
        var moduleReferences = ReadProjectReferences(projectName)
            .Select(GetProjectName)
            .Where(name => name.StartsWith("EnglishLiveTutor.Modules.", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(moduleReferences);
    }

    private static IReadOnlyList<string> ReadProjectReferences(string projectName)
    {
        // Shared building blocks live under src/Shared/ (ADR-0006, P5j).
        return projectName.StartsWith("EnglishLiveTutor.Shared.", StringComparison.Ordinal)
            ? ReadProjectReferences("src", "Shared", projectName, $"{projectName}.csproj")
            : ReadProjectReferences("src", projectName, $"{projectName}.csproj");
    }

    private static IReadOnlyList<string> ReadProjectReferences(
        params string[] projectPathSegments)
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var projectPath = Path.Combine([root, .. projectPathSegments]);
        return ReadProjectReferencesFromFile(projectPath);
    }

    private static IReadOnlyList<string> ReadProjectReferencesFromFile(string projectPath)
    {
        var project = XDocument.Load(projectPath);

        return project
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadPackageReferencesFromFile(string projectPath)
    {
        var project = XDocument.Load(projectPath);

        return project
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static HashSet<string> GetAllowedProjects(
        ModuleName module,
        ModuleProjectLayer layer)
    {
        if (layer == ModuleProjectLayer.Domain)
        {
            return new HashSet<string>(StringComparer.Ordinal)
            {
                "EnglishLiveTutor.Shared.Domain"
            };
        }

        var allowedProjects = new HashSet<string>(StringComparer.Ordinal)
        {
            "EnglishLiveTutor.Shared.Domain",
            "EnglishLiveTutor.Shared.Application"
        };

        if (layer != ModuleProjectLayer.Presentation)
        {
            foreach (var dependency in ModuleMap.AllowedDependencies[module])
            {
                allowedProjects.Add(GetContractsProjectName(dependency));
            }
        }

        if (layer != ModuleProjectLayer.Contracts)
        {
            allowedProjects.Add(GetContractsProjectName(module));
        }

        if (layer is ModuleProjectLayer.Application or ModuleProjectLayer.Infrastructure or
            ModuleProjectLayer.Presentation)
        {
            // Progress module has no Domain project (ADR-0006: 3-project module exception).
            if (module != ModuleName.Progress)
            {
                allowedProjects.Add(GetProjectName(module, "Domain"));
            }
        }

        if (layer is ModuleProjectLayer.Infrastructure or ModuleProjectLayer.Presentation)
        {
            allowedProjects.Add(GetProjectName(module, "Application"));
        }

        if (layer == ModuleProjectLayer.Infrastructure)
        {
            allowedProjects.Add("EnglishLiveTutor.Shared.Infrastructure");
        }

        if (layer == ModuleProjectLayer.Presentation)
        {
            allowedProjects.Add("EnglishLiveTutor.Shared.Presentation");
        }

        return allowedProjects;
    }

    private static ModuleProjectLayer GetProjectLayer(
        string projectName,
        ModuleName module)
    {
        if (projectName.EndsWith(".Domain", StringComparison.Ordinal))
        {
            return ModuleProjectLayer.Domain;
        }

        if (projectName.EndsWith(".Contracts", StringComparison.Ordinal))
        {
            return ModuleProjectLayer.Contracts;
        }

        if (projectName.EndsWith(".Infrastructure", StringComparison.Ordinal))
        {
            return ModuleProjectLayer.Infrastructure;
        }

        if (projectName.EndsWith(".Presentation", StringComparison.Ordinal))
        {
            return ModuleProjectLayer.Presentation;
        }

        Assert.True(
            projectName.EndsWith(".Application", StringComparison.Ordinal) ||
            projectName.Equals($"EnglishLiveTutor.Modules.{module}", StringComparison.Ordinal),
            $"Unknown module project layer for '{projectName}'.");
        return ModuleProjectLayer.Application;
    }

    private static string GetContractsProjectName(ModuleName module)
    {
        return $"EnglishLiveTutor.Modules.{module}.Contracts";
    }

    private static string GetProjectName(ModuleName module, string layer)
    {
        return $"EnglishLiveTutor.Modules.{module}.{layer}";
    }

    private static string GetProjectName(string reference)
    {
        return Path.GetFileNameWithoutExtension(reference.Replace('\\', '/'));
    }

    private static bool HasExcludedSegment(string path)
    {
        return path
            .Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment is "bin" or "obj");
    }

    private enum ModuleProjectLayer
    {
        Application,
        Contracts,
        Domain,
        Infrastructure,
        Presentation
    }
}
