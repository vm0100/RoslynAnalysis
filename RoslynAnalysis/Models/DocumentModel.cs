using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using AvalonDock.Layout;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ICSharpCode.AvalonEdit.Document;

using RoslynAnalysis.Consts;
using RoslynAnalysis.Convert;

using System.Windows.Threading;
using System.Threading;
using RoslynAnalysis.Core;

namespace RoslynAnalysis.Models;

public partial class DocumentModel : LayoutDocument
{
    private string _filePath = null;
    private TextDocument _textContent;
    private TextDocument _translateTextContent;

    public DocumentModel()
    {
        Title = "空文档";
        CanClose = false;
        CanFloat = false;

        TextContent = new TextDocument();
        TranslateTextContent = new TextDocument();

        Observable.FromEventPattern(TextContent, "TextChanged")
                  .Throttle(TimeSpan.FromMilliseconds(500))
                  .ObserveOn(SynchronizationContext.Current)
                  .Select(evt => ((IDocument)evt.Sender).Text).Subscribe(TextContent_TextChanged);
    }

    /// <summary>
    /// 打开文档
    /// </summary>
    /// <param name="filePath"></param>
    public DocumentModel(string filePath) : this()
    {
        CanClose = true;
        FilePath = filePath;
        IconSource = new BitmapImage(new Uri("/Resources/CSFileNode.png", UriKind.Relative));
    }

    /// <summary>
    /// 文档变更事件
    /// </summary>
    /// <param name="sourceCode"></param>
    private void TextContent_TextChanged(string sourceCode)
    {
        var translateCode = ConvertFactory.Create(ConvertEnum.Java).GenerateCode(sourceCode);
        TranslateTextContent.Text = translateCode;
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;

                RaisePropertyChanged(nameof(FilePath));
                RaisePropertyChanged(nameof(FileName));
                RaisePropertyChanged(nameof(Title));
                RaisePropertyChanged(nameof(TextContent));

                ContentId = _filePath;

                if (_filePath.IsNotNullOrWhiteSpace())
                {
                    Title = Path.GetFileName(_filePath);

                    if (File.Exists(_filePath))
                    {
                        TextContent.Text = File.ReadAllText(_filePath);
                    }
                }
            }
        }
    }

    public string FileName
    { get => Title; set { if (Title != value) { Title = value; } } }

    public TextDocument TextContent
    {
        get => _textContent;
        set
        {
            if (_textContent != value)
            {
                _textContent = value;
                RaisePropertyChanged(nameof(TextContent));
            }
        }
    }

    public TextDocument TranslateTextContent
    {
        get => _translateTextContent;
        set
        {
            if (_translateTextContent != value)
            {
                _translateTextContent = value;
                RaisePropertyChanged(nameof(TranslateTextContent));
            }
        }
    }

    /// <summary>
    /// 文档关闭
    /// </summary>
    public ICommand CloseCommand => new RelayCommand(() =>
    {
        WeakReferenceMessenger.Default.Send(this, MessengerTokens.DocumentClose);
    });

    public bool InvokeRequired { get; }
}