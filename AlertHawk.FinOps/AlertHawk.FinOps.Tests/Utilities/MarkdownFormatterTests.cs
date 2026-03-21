using FinOpsToolSample.Utilities;

namespace AlertHawk.FinOps.Tests.Utilities;

public class MarkdownFormatterTests
{
    [Fact]
    public void ToPlainText_RemovesHeadersBoldLinksAndCode()
    {
        const string md = """
            # Title
            **bold** and *italic* and [link](https://x.com)
            `code`
            """;
        var plain = MarkdownFormatter.ToPlainText(md);
        Assert.DoesNotContain("#", plain);
        Assert.DoesNotContain("**", plain);
        Assert.DoesNotContain("http", plain);
        Assert.Contains("bold", plain);
        Assert.Contains("link", plain);
        Assert.Contains("code", plain);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, MarkdownFormatter.ToPlainText(input!));
    }

    [Fact]
    public void FormatForConsole_FormatsHeaderLevels()
    {
        var md = """
            # Main
            ## Sub
            ### Detail
            - item
            """;
        var formatted = MarkdownFormatter.FormatForConsole(md);
        Assert.Contains("═══ MAIN ═══", formatted);
        Assert.Contains("─── Sub ───", formatted);
        Assert.Contains("• Detail", formatted);
        Assert.Contains("  - item", formatted);
    }

    [Fact]
    public void GetSummary_TruncatesLongPlainText()
    {
        var longLine = string.Join(" ", Enumerable.Repeat("word", 200));
        var md = $"# H\n{longLine}";
        var summary = MarkdownFormatter.GetSummary(md, maxLength: 50);
        Assert.True(summary.Length <= 50);
        Assert.EndsWith("...", summary);
    }

    [Fact]
    public void PrepareForDisplay_NormalizesNewlines()
    {
        var stored = "a\r\nb\nc";
        var display = MarkdownFormatter.PrepareForDisplay(stored);
        Assert.DoesNotContain("\r\n\r\n", display);
        Assert.Contains(Environment.NewLine, display);
    }

    [Fact]
    public void PrepareForStorage_ReturnsInputUnchanged()
    {
        const string md = "# x\nline";
        Assert.Same(md, MarkdownFormatter.PrepareForStorage(md));
    }
}
