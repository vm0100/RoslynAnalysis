using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

using RoslynAnalysis.Models;

namespace RoslynAnalysis.Converters
{
    public class StringEmptyVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && (Visibility)value == Visibility.Collapsed;
        }
    }
}