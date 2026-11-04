using System.Text;
using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

/// <summary>
/// Guards against UTF-8 text that was decoded as Latin-1/Windows-1252 and saved again
/// (e.g. "Không" → "KhÃ´ng"). Moving files between projects has caused this before.
/// </summary>
public sealed class SourceEncodingTests
{
    // Byte pairs that only appear when UTF-8 Vietnamese is mis-decoded as Latin-1:
    // "Ã" + Latin-1 supplement, "áº"/"á»" (U+1Exx), "Ä"/"Æ" + continuation byte, "â€" (smart punctuation).
    private static readonly Regex Mojibake = new(
        "Ã[ -¿]|á[º»]|Ä[\u0080-¿]|Æ[\u0080-¿]|â€",
        RegexOptions.CultureInvariant);

    [Fact]
    public void SourceFiles_ContainNoMojibake()
    {
        var root = ModuleBoundaryScanner.FindSolutionRoot();
        var offenders = new List<string>();

        foreach (var directory in new[] { "src", "tests" })
        {
            var path = Path.Combine(root, directory);
            foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Contains("/obj/", StringComparison.Ordinal) ||
                    relative.Contains("/bin/", StringComparison.Ordinal) ||
                    relative.EndsWith("SourceEncodingTests.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (Mojibake.IsMatch(lines[i]))
                    {
                        offenders.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Mis-decoded UTF-8 text found:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Theory]
    [InlineData("KhÃ´ng tÃ¬m tháº¥y", true)]
    [InlineData("Không tìm thấy rubric.", false)]
    [InlineData("Từ vựng tiếng Việt: ăn, ơ, ư, đ", false)]
    public void MojibakePattern_DetectsOnlyMisDecodedText(string text, bool expected)
    {
        Assert.Equal(expected, Mojibake.IsMatch(text));
    }
}
