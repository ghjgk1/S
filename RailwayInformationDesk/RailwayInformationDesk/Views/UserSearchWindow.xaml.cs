// Views/UserSearchWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RailwayInformationDesk.Data;
using RailwayInformationDesk.Models;
using System.Windows;
using System.Windows.Controls;

namespace RailwayInformationDesk.Views;

public partial class UserSearchWindow : Window
{
    private readonly RailwayInformationDeskContext _context = new();
    private List<Station> _stationsInRegion = [];
    private Station? _from, _to;

    public UserSearchWindow()
    {
        InitializeComponent();
        LoadRegions();
    }

    private async void LoadRegions()
    {
        _stationsInRegion = await _context.Stations.Where(s => s.Region != null).OrderBy(s => s.Name).ToListAsync();
        FromStation.ItemsSource = _stationsInRegion;
        ToStation.ItemsSource = _stationsInRegion;
        FromStation.DisplayMemberPath = "Name";
        ToStation.DisplayMemberPath = "Name";
    }

    private async void RegionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (FromStation.SelectedItem is not Station from || ToStation.SelectedItem is not Station to)
        {
            MessageBox.Show("Выберите станции.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _from = from;
        _to = to;
        await LoadTrips();
    }

    private async Task LoadTrips()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var allTemplates = await _context.ScheduleTemplates
            .Include(t => t.Route)
            .Include(t => t.TrainType)
            .Include(t => t.ScheduleTemplateStops)
                .ThenInclude(ts => ts.Station)
            .Include(t => t.ScheduleTemplateStops)
                .ThenInclude(ts => ts.ActualSchedules)
            .ToListAsync();

        var viewModels = new List<UserTripViewModel>();

        foreach (var template in allTemplates)
        {
            // 1. Находим остановки "откуда" и "куда"
            var fromStop = template.ScheduleTemplateStops.FirstOrDefault(s => s.StationId == _from!.Id);
            var toStop = template.ScheduleTemplateStops.FirstOrDefault(s => s.StationId == _to!.Id);

            // 2. Проверяем наличие и порядок
            if (fromStop == null || toStop == null || fromStop.StopOrder >= toStop.StopOrder)
                continue;

            // 3. Находим рейс на сегодня
            var tripInstance = await _context.TripInstances
                .FirstOrDefaultAsync(ti => ti.TemplateId == template.Id && ti.TripDate == today);
            if (tripInstance == null) continue;

            // 4. Ищем задержку на участке
            ActualSchedule? delayedRecord = null;
            Station? delayedStation = null;

            var stopsOnRoute = template.ScheduleTemplateStops
                .Where(s => s.StopOrder >= fromStop.StopOrder && s.StopOrder <= toStop.StopOrder)
                .OrderBy(s => s.StopOrder);

            foreach (var stop in stopsOnRoute)
            {
                var actual = stop.ActualSchedules
                    .FirstOrDefault(a => a.TripInstanceId == tripInstance.Id);

                if (actual?.DelayMinutes > 0)
                {
                    delayedRecord = actual;
                    delayedStation = stop.Station;
                    break;
                }
            }

            // 5. Формируем статус
            string status = "По расписанию";
            if (delayedRecord != null && delayedStation != null)
            {
                status = $"С опозданием на {delayedRecord.DelayMinutes} мин со ст. {delayedStation.Name}";
            }

            viewModels.Add(new UserTripViewModel
            {
                Template = template,
                TripDate = today,
                DepartureTime = fromStop.DepartureTime,
                ArrivalTime = toStop.ArrivalTime,
                Platform = fromStop.Platform ?? "—",
                Status = status
            });
        }

        TripsGrid.ItemsSource = viewModels.OrderBy(vm => vm.DepartureTime).ToList();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            new LoginWindow().Show();
            this.Close();
        }
        catch { }
    }

    private void TogglePastTrips(object sender, RoutedEventArgs e) { /* уже фильтруется по времени */ }
}

public class UserTripViewModel
{
    public ScheduleTemplate Template { get; set; } = null!;
    public DateOnly TripDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public string Platform { get; set; } = "—";

    // Новые свойства
    public string Status { get; set; } = "По расписанию";
    public bool IsDelayed { get; set; }

    public string DepartureDisplay => DepartureTime.HasValue ? $"{TripDate:dd.MM} {DepartureTime:HH:mm}" : "—";
    public string ArrivalDisplay => ArrivalTime.HasValue ? $"{TripDate:dd.MM} {ArrivalTime:HH:mm}" : "—";
}