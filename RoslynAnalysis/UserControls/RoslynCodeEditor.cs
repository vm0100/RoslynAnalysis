using System;
using System.Linq;
using System.Text;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

namespace RoslynAnalysis.UserControls;

public class RoslynCodeEditor : TextEditor
{
    private SearchPanel? _searchReplacePanel;

    public RoslynCodeEditor()
    {
        _searchReplacePanel = SearchPanel.Install(this);
        TextArea.TextView.ElementGenerators.Add(new TruncateLongLines());

        Options = new TextEditorOptions
        {
            ConvertTabsToSpaces = true,
            AllowScrollBelowDocument = true,
            IndentationSize = 4,
            EnableEmailHyperlinks = false,
        };
        //var highlighting = HighlightingManager.Instance.GetDefinition("C#");
        //var highlightColors = new ClassificationHighlightColors();
        //foreach (var kvp in highlightColors.GetOrCreateMap())
        //{
        //    highlighting.GetNamedColor(kvp.Key).Foreground = kvp.Value.Foreground;
        //}
        //SyntaxHighlighting = highlighting;
    }
}