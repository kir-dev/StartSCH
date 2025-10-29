using System.Collections;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Markdig;

namespace StartSch;

// TODO: Perf: make TextContent properties lazy
// TODO: TextContent: limit excerpt length
public class TextContent
{
    private static readonly HtmlSanitizer DefaultHtmlSanitizer = new();
    private static readonly HtmlSanitizer ExcerptHtmlSanitizer = CreateExcerptHtmlSanitizer();
    private static readonly HtmlSanitizer TextOnlyHtmlSanitizer = CreateTextOnlyHtmlSanitizer();
    private static readonly MinifyMarkupFormatter MinifyMarkupFormatter = new();

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UseGridTables()
        .UseMediaLinks()
        .UsePipeTables()
        .Build();

    // HtmlContent = content.MdToHtml().Sanitize(...)
    // Excerpt not set
    //     HtmlExcerpt = HtmlContent.Sanitize(...)
    //     TextExcerpt = HtmlExcerpt.Sanitize(...).HtmlToTxt()
    // Excerpt set
    //     HtmlExcerpt = excerpt.MdToHtml().Sanitize(...)
    //     TextExcerpt = HtmlExcerpt.Sanitize(...).HtmlToTxt()
    public TextContent(string? contentMarkdown, string? excerptMarkdown)
    {
        HtmlContent = HtmlExcerpt = TextExcerpt = "";
        UseExcerpt();
        UseContent();
        TextExcerpt = HttpUtility.HtmlDecode(TextExcerpt).AsSpan().ToExcerpt();
        return;

        void UseExcerpt()
        {
            if (string.IsNullOrWhiteSpace(excerptMarkdown))
                return;

            string excerptHtml = MarkdownToHtml(excerptMarkdown);
            using IHtmlDocument excerptDom = ExcerptHtmlSanitizer.SanitizeDom(excerptHtml);
            if (excerptDom.Body == null)
                return;

            HtmlExcerpt = excerptDom.Body.ChildNodes.ToHtml(DefaultHtmlSanitizer.OutputFormatter);

            TextOnlyHtmlSanitizer.SanitizeDom(excerptDom, excerptDom.Body);
            TextExcerpt = excerptDom.Body.ChildNodes.ToHtml(DefaultHtmlSanitizer.OutputFormatter);
        }

        void UseContent()
        {
            if (string.IsNullOrWhiteSpace(contentMarkdown))
                return;

            string contentHtml = MarkdownToHtml(contentMarkdown);
            using IHtmlDocument contentDom = DefaultHtmlSanitizer.SanitizeDom(contentHtml);
            if (contentDom.Body == null)
                return;

            if (!HasContent(contentDom))
                return;

            HtmlContent = contentDom.Body.ChildNodes.ToHtml(MinifyMarkupFormatter);

            if (!string.IsNullOrWhiteSpace(excerptMarkdown))
                return;

            ExcerptHtmlSanitizer.SanitizeDom(contentDom, contentDom.Body);
            HtmlExcerpt = contentDom.Body.ChildNodes.ToHtml(DefaultHtmlSanitizer.OutputFormatter);

            TextOnlyHtmlSanitizer.SanitizeDom(contentDom, contentDom.Body);
            TextExcerpt = contentDom.Body.ChildNodes.ToHtml(DefaultHtmlSanitizer.OutputFormatter);
        }
    }

    /// Should be rendered as HTML
    public string HtmlContent { get; private set; }

    /// Should be rendered as HTML
    public string HtmlExcerpt { get; private set; }

    /// Must be rendered as text
    public string TextExcerpt { get; private set; }

    private static string MarkdownToHtml(string markdown) => Markdown.ToHtml(markdown, MarkdownPipeline);

    private static HtmlSanitizer CreateExcerptHtmlSanitizer()
    {
        HtmlSanitizer sanitizer = new();

        sanitizer.AllowedAttributes.Clear();
        IEnumerable<string> attrs = ["href", "style"];
        foreach (var attr in attrs) sanitizer.AllowedAttributes.Add(attr);

        sanitizer.AllowedTags.Clear();
        IEnumerable<string> tags =
        [
            // based on https://github.com/mganss/HtmlSanitizer/blob/1c05d6ccf98cd69ef08f2b4942f03fd680f78a80/src/HtmlSanitizer/HtmlSanitizerDefaults.cs#L32
            "a", "b",
            "br",
            "code",
            "em", "font",
            "i", "kbd",
            "p", "pre", "q", "s", "samp",
            "small", "span", "strike", "strong", "sub", "sup",
            "tt", "u",
        ];
        foreach (var tag in tags) sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedCssProperties.Clear();
        IEnumerable<string> classes =
        [
            "color",
            "font-family",
            "font-weight",
        ];
        foreach (var c in classes) sanitizer.AllowedTags.Add(c);

        sanitizer.KeepChildNodes = true;
        return sanitizer;
    }

    private static HtmlSanitizer CreateTextOnlyHtmlSanitizer()
    {
        HtmlSanitizer sanitizer = new();
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedTags.Clear();
        sanitizer.KeepChildNodes = true;
        return sanitizer;
    }

    /// Removes nodes that don't contain any content (non-whitespace text).
    /// <returns>
    /// Whether the given node should be kept.
    /// </returns>
    /// <remarks>
    /// Should get rid of any useless whitespace that can mess with formatting when using <c>white-space: pre-wrap</c>.
    /// </remarks>
    private static bool HasContent(INode node)
    {
        if (node is IText text && !string.IsNullOrWhiteSpace(text.Data))
            return true;

        if (!node.HasChildNodes)
            return false;

        BitArray keepNode = new(node.ChildNodes.Length);
        bool keepCurrent = false;
        for (int i = 0; i < node.ChildNodes.Length; i++)
        {
            INode childNode = node.ChildNodes[i];
            bool hasContent = HasContent(childNode);
            keepNode[i] = hasContent;
            keepCurrent |= hasContent;
        }

        if (!keepCurrent)
            return false;

        for (int i = keepNode.Count - 1; i >= 0; i--)
            if (!keepNode[i])
                node.ChildNodes[i].RemoveFromParent();

        return true;
    }
}
