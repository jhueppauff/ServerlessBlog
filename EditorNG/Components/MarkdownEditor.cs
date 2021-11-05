using Markdig;
using Microsoft.AspNetCore.Components;

namespace EditorNG.Components
{
    public class MarkdownEditorBase : ComponentBase
    {
        public string Body { get; set; } = string.Empty;
        public string Preview => Markdown.ToHtml(Body);
    }

}