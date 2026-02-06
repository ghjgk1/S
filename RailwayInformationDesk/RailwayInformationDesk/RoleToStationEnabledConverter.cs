// Converters/RoleToStationEnabledConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace RailwayInformationDesk.Converters;

public class RoleToStationEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string role && role == "Дежурный";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}