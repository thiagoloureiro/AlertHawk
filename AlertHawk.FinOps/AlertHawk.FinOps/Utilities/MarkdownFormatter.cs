using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FinOpsToolSample.Utilities
{
    public static class MarkdownFormatter
    {
        /// <summary>
        /// Converts markdown to plain text for console display
        /// </summary>
        public static string ToPlainText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var text = markdown;

            // Remove markdown headers (# ## ###)
            text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);

            // Remove bold/italic markers
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1"); // Bold
            text = Regex.Replace(text, @"\*(.+?)\*", "$1"); // Italic
            text = Regex.Replace(text, @"__(.+?)__", "$1"); // Bold
            text = Regex.Replace(text, @"_(.+?)_", "$1"); // Italic

            // Remove links [text](url) -> text
            text = Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");

            // Remove inline code markers
            text = Regex.Replace(text, @"`(.+?)`", "$1");

            return text;
        }

        /// <summary>
        /// Formats markdown for better console output
        /// </summary>
        public static string FormatForConsole(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var lines = markdown.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            var formatted = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // Headers
                if (trimmed.StartsWith("# "))
                {
                    formatted.AppendLine();
                    formatted.AppendLine("═══ " + trimmed.Substring(2).ToUpper() + " ═══");
                    formatted.AppendLine();
                }
                else if (trimmed.StartsWith("## "))
                {
                    formatted.AppendLine();
                    formatted.AppendLine("─── " + trimmed.Substring(3) + " ───");
                }
                else if (trimmed.StartsWith("### "))
                {
                    formatted.AppendLine();
                    formatted.AppendLine("• " + trimmed.Substring(4));
                }
                // Lists
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    formatted.AppendLine("  " + trimmed);
                }
                // Numbered lists
                else if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    formatted.AppendLine("  " + trimmed);
                }
                // Code blocks
                else if (trimmed.StartsWith("```"))
                {
                    formatted.AppendLine("───────────────────────────────────────");
                }
                // Normal text
                else
                {
                    formatted.AppendLine(line);
                }
            }

            return formatted.ToString();
        }

        /// <summary>
        /// Gets a preview/summary from markdown
        /// </summary>
        public static string GetSummary(string markdown, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var plainText = ToPlainText(markdown);
            var lines = plainText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Take first few meaningful lines
            var summary = string.Join(" ", lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(5));

            if (summary.Length > maxLength)
            {
                summary = summary.Substring(0, maxLength - 3) + "...";
            }

            return summary;
        }

        /// <summary>
        /// Prepares markdown for database storage (preserves all formatting)
        /// </summary>
        public static string PrepareForStorage(string markdown)
        {
            // Markdown should be stored as-is
            // SQL Server nvarchar(max) will preserve newlines
            return markdown;
        }

        /// <summary>
        /// Retrieves markdown from database and ensures newlines are proper
        /// </summary>
        public static string PrepareForDisplay(string storedMarkdown)
        {
            if (string.IsNullOrWhiteSpace(storedMarkdown))
                return string.Empty;

            // Ensure proper newlines for the current platform
            return storedMarkdown
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);
        }
    }
}
