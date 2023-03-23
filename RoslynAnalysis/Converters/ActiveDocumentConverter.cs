using System;
using System.Linq;
using System.Text;
using System.Windows.Data;

using RoslynAnalysis.Models;

namespace RoslynAnalysis.Converters
{
    public class ActiveDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DocumentModel fileModel)
            {
                return value;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DocumentModel)
                return value;

            return Binding.DoNothing;
        }
    }
}