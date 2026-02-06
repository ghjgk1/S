using RailwayInformationDesk.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace RailwayInformationDesk.Converters;

public class IdToNameConverter : IValueConverter
{
    public static ObservableCollection<Station> Stations { get; set; } = [];
    public static ObservableCollection<Route> Routes { get; set; } = [];
    public static ObservableCollection<TrainType> TrainTypes { get; set; } = [];
    public static ObservableCollection<ScheduleTemplate> Templates { get; set; } = [];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int id) return "—";

        return parameter?.ToString() switch
        {
            "Station" => Stations.FirstOrDefault(s => s.Id == id)?.Name ?? "—",
            "Route" => Routes.FirstOrDefault(r => r.Id == id)?.Name ?? "—",
            "TrainType" => TrainTypes.FirstOrDefault(t => t.Id == id)?.Name ?? "—",
            "Template" => Templates.FirstOrDefault(t => t.Id == id)?.Route.Name ?? "—",
            _ => "—"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}