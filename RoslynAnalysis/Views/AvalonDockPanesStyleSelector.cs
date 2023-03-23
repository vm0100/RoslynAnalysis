using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using RoslynAnalysis.Models;

namespace RoslynAnalysis.Views
{
    public class AvalonDockPanesStyleSelector : StyleSelector
    {
        public Style DocumentViewStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is DocumentModel)
                return DocumentViewStyle;

            return base.SelectStyle(item, container);
        }
    }
}