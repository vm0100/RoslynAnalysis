using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

using AvalonDock.Themes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.CodeAnalysis;

using RoslynAnalysis.Consts;
using RoslynAnalysis.Convert;
using RoslynAnalysis.Core;
using RoslynAnalysis.Models;

namespace RoslynAnalysis.ViewModels;

[ServiceDescriptor(typeof(MainWindowViewModel))]
public partial class MainWindowViewModel : ObservableRecipient
{
    private static readonly string _tag = "v1.0.0";

    [ObservableProperty]
    private string _versionInfo;

    [ObservableProperty]
    private string _title = "C#转Java小工具~";

    [ObservableProperty]
    private Theme _theme = new Vs2013LightTheme();

    [ObservableProperty]
    private string _contentTitle;

    [ObservableProperty]
    private string _dirPath;

    [ObservableProperty]
    private DocumentModel _activeDocument;

    [ObservableProperty]
    private ObservableCollection<DirectoryTreeItem> _directoryTreeItems = new ObservableCollection<DirectoryTreeItem>();

    [ObservableProperty]
    private ObservableCollection<DocumentModel> _openFiles = new ObservableCollection<DocumentModel>();

    private string _searchInput;
    public string SearchInput
    {
        get => _searchInput;
        set
        {
            SetProperty(ref _searchInput, value);
            _searchInputSubject.OnNext(value);
        }
    }

    private readonly Subject<string> _searchInputSubject = new();

    public MainWindowViewModel()
    {
        IsActive = true;

        _searchInputSubject.Throttle(TimeSpan.FromMilliseconds(500))
            .Select(searchText => SearchDirectoryTree(DirPath, searchText.IsNotNullOrWhiteSpace(),
                                                               searchText.IsNullOrWhiteSpace() ? "*?.cs" : "*" + searchText + "*?.cs"))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(searchTreeItemArr =>
            {
                DirectoryTreeItems.Clear();
                foreach (var dirInfo in searchTreeItemArr)
                {
                    DirectoryTreeItems.Add(dirInfo);
                }
            });

#if DEBUG

        DirPath = "D:\\代码备份\\售楼系统\\src";
        if (DirPath.Contains("bin"))
        {
            DirPath = DirPath[0..(DirPath.IndexOf("bin"))];
        }

        _searchInputSubject.OnNext("");

        var defaultDocument = new DocumentModel();
        defaultDocument.TextContent.Text = File.ReadAllText(@"./CSharpCode.txt");
        _openFiles.Add(defaultDocument);

#endif
        //githubClient.Repository.Release.GetAll();

        // 检测版本更新
        //Task.Run(async () =>
        //{
        //    if (VersionInfo.IsNullOrWhiteSpace())
        //    {
        //        VersionInfo = await GetNewReleaseVersionAsync();
        //    }

        //    if (VersionInfo == _tag)
        //    {
        //        VersionInfo = "当前版本：" + VersionInfo;
        //        return;
        //    }

        //    VersionInfo = "存在新版本：" + VersionInfo;
        //});
    }

    protected override void OnActivated()
    {
        Messenger.Register<MainWindowViewModel, DocumentModel, string>(this, MessengerTokens.DocumentClose, (sender, file) =>
        {
            OpenFiles.Remove(file);
        });

        base.OnActivated();
    }

    [RelayCommand]
    public void SelectDirectory()
    {
        var folderBrowserDialog = new FolderBrowserDialog();
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        DirPath = folderBrowserDialog.SelectedPath;
        _searchInputSubject.OnNext("");
    }

    [RelayCommand]
    public void DirTreeViewSelectedItemChanged(DirectoryTreeItem selectedItem)
    {
        if (selectedItem == null || selectedItem.IsDirectory)
        {
            return;
        }
        var activeFile = OpenFiles.FirstOrDefault(f => f.IsActive);
        if (activeFile != null)
        {
            activeFile.IsActive = false;
            activeFile.IsSelected = false;
        }

        activeFile = OpenFiles.FirstOrDefault(f => f.FilePath == selectedItem.FullPath);
        if (activeFile != null)
        {
            activeFile.IsActive = true;
            activeFile.IsSelected = true;
            ActiveDocument = activeFile;
            return;
        }

        activeFile = new DocumentModel(selectedItem.FullPath);
        activeFile.IsActive = true;
        activeFile.IsSelected = true;
        OpenFiles.Add(activeFile);
        ActiveDocument = activeFile;
    }

    [RelayCommand]
    public void ConvertTest()
    {
        var dirList = DirectoryTreeItems.SelectMany(r => r.Children).Where(r => r != null).ToList();
        try
        {
            while (dirList.Count > 0)
            {
                var fileList = dirList.Where(r => r.IsFile).ToList();
                if (fileList.Count < 1)
                {
                    dirList = dirList.SelectMany(r => r.Children).Where(r => r != null).ToList();
                    continue;
                }

                IAnalysisConvert AnalysisConvert = ConvertFactory.Create(ConvertEnum.Java);
                foreach (var fileInfo in fileList)
                {
                    try
                    {
                        AnalysisConvert.GenerateCode(File.ReadAllText(fileInfo.FullPath));
                    }
                    catch (System.Exception)
                    {
                        DirTreeViewSelectedItemChanged(fileInfo);
                        throw;
                    }
                }

                dirList = dirList.SelectMany(r => r.Children).Where(r => r != null).ToList();
            }
            // JavaCode.Text = "验证成功， 没有报错的！！";
        }
        catch (System.Exception)
        {
        }
    }

    private readonly string[] ignore_directory_suffix = new string[] { "\\bin\\", "\\obj\\", "\\.git\\", "\\UnitTest\\" };
    private readonly string[] ignore_file_suffix = new string[] { "Program", "AppRazorPageBase", "AssemblyInfo", "Test" };

    public IEnumerable<DirectoryTreeItem> SearchDirectoryTree(string path, bool isExpanded = false, string searchPattern = "*?.cs")
    {
        path = path.TrimEnd('\\');

        if (!Directory.Exists(path))
        {
            yield break;
        }

        var treeItemList = Directory.EnumerateFiles(DirPath, searchPattern, SearchOption.AllDirectories)
                                    .AsParallel()
                                    .Where(f => !f.InContains(ignore_directory_suffix) && !f.GetFileNameWithoutExtension().InEndsWith(ignore_file_suffix))
                                    .OrderBy(f => f)
                                    .Select(f => new DirectoryTreeItem(f, true))
                                    .ToList();

        var searchDirectoryInfos = new List<DirectoryTreeItem>();
        while (treeItemList.Any())
        {
            int hierarchyCount = treeItemList.Max(r => r.HierarchyCount);
            var newDirInfos = new List<DirectoryTreeItem>();
            foreach (DirectoryTreeItem item in treeItemList.OrderBy(a => !a.IsDirectory).ThenBy(a => a.Name))
            {
                if (item.HierarchyCount < hierarchyCount)
                {
                    newDirInfos.Add(item);
                    continue;
                }

                if (item.IsDirectory && item.FullPath == path)
                {
                    foreach (var treeInfo in item.Children)
                    {
                        yield return treeInfo;
                    }
                    continue;
                }

                var dirName = item.IsFile ? Path.GetDirectoryName(item.FullPath) : Path.GetDirectoryName(item.FullPath);
                var newDirInfo = newDirInfos.FirstOrDefault(d => d.FullPath == dirName);

                if (newDirInfo == null)
                {
                    newDirInfo = new DirectoryTreeItem(dirName, false, isExpanded);
                    newDirInfos.Add(newDirInfo);
                }

                newDirInfo.Children.Add(item);
            }

            treeItemList = newDirInfos;
        }
    }

    //public async Task<string> GetNewReleaseVersionAsync()
    //{
    //    var githubClient = new GitHubClient(new ProductHeaderValue("RoslynAnalysis"));
    //    var repository = await githubClient.Repository.Release.GetAll(596033079, new ApiOptions() { PageSize = 1, PageCount = 1 });
    //    return repository.OrderBy(a => a.PublishedAt).First().Name;
    //}

    //public bool IsNeedUpgrade { get; set; }
}