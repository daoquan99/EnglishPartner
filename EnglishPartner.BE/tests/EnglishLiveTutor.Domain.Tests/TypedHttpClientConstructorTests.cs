using EnglishLiveTutor.Infrastructure.Lessons;

namespace EnglishLiveTutor.Domain.Tests;

public class TypedHttpClientConstructorTests
{
    // HttpClientFactory activates typed clients with ActivatorUtilities, which fails at request time
    // ("A suitable constructor ... could not be located") when several public constructors are satisfiable.
    [Fact]
    public void TypedHttpClients_ShouldExposeExactlyOnePublicConstructor()
    {
        // Registered as singletons (not typed clients); the HttpMessageHandler overload exists for tests.
        var notTypedClients = new[] { "FastTextTranslatorFallback", "GrammarCorrectionAnalyzerService" };

        var offenders = typeof(LessonAiGrader).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && !notTypedClients.Contains(t.Name))
            .Where(t => t.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpClient))))
            .Where(t => t.GetConstructors().Length > 1)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }
}
