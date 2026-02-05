using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BZTerrainEditor.Converters
{
    public class BothNonEmptyToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;
            if (values[0] is int count1 && values[1] is int count2)
            {
                return (count1 > 0 && count2 > 0) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}