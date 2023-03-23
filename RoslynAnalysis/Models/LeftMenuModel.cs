using System;
using System.Linq;
using System.Text;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;

namespace RoslynAnalysis.Models;

public class LeftMenuModel : ObservableRecipient
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 图标名称
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public Visibility IsVisible { get; set; }

    /// <summary>
    /// 页面名称
    /// </summary>
    public string ViewName { get; set; }

    public LeftMenuModel()
    {
    }

    public LeftMenuModel(string name, string viewName) : this()
    {
        this.Name = name;
        this.ViewName = viewName;
    }

    public LeftMenuModel(string name, string viewName, string imageName) : this(name, viewName)
    {
        this.Name = name;
        this.ViewName = viewName;
        this.ImageName = imageName;
    }

    public LeftMenuModel(string name, string viewName, string imageName, bool isVisible) : this(name, viewName, imageName)
    {
        this.IsVisible = isVisible ? Visibility.Visible : Visibility.Hidden;
    }
}