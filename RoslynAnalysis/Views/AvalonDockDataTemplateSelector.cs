using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using RoslynAnalysis.Models;

namespace RoslynAnalysis.Views
{
    public class AvalonDockDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DocumentViewTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DocumentModel)
            {
                return DocumentViewTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}