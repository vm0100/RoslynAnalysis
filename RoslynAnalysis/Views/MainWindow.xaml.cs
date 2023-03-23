using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace RoslynAnalysis.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void RestoreDownClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            SetCaptionHeight();
        }

        private void HeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetCaptionHeight();
        }

        private void SetCaptionHeight()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    chrome.CaptionHeight = header.ActualHeight + BorderThickness.Top - chrome.ResizeBorderThickness.Top;
                    break;

                case WindowState.Maximized:
                    chrome.CaptionHeight = header.ActualHeight - BorderThickness.Top;
                    break;
            }
        }
    }
}