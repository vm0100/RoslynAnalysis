using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RoslynAnalysis.Models;

public partial class DirectoryTreeItem : ObservableRecipient
{
    public DirectoryTreeItem()
    {
        Icon = DependencyProperty.UnsetValue;
        children = new ObservableCollection<DirectoryTreeItem>();
    }

    public DirectoryTreeItem(string path) : this()
    {
        FullPath = path;
        HierarchyCount = path.Count(c => c == '\\');
        Name = path.Contains('.') ? Path.GetFileName(path) : path[(path.LastIndexOf('\\') + 1)..];
    }

    public DirectoryTreeItem(string path, bool isFile) : this(path)
    {
        IsFile = isFile;
        IsDirectory = !isFile;

        Icon = IsDirectory ? "/Resources/FolderClosed.png" : "/Resources/CSFileNode.png";
    }

    public DirectoryTreeItem(string path, bool isFile, bool isExpanded) : this(path, isFile)
    {
        IsExpanded = !isFile && isExpanded;
    }

    public object Icon { get; }
    public string Name { get; }
    public string FullPath { get; }
    public bool IsFile { get; }
    public bool IsDirectory { get; }
    public bool IsExpanded { get; set; }
    public int HierarchyCount { get; }

    [ObservableProperty]
    private ObservableCollection<DirectoryTreeItem> children;
}