// Views/DispatcherWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using RailwayInformationDesk.Data;
using RailwayInformationDesk.Models;
using System.Windows;
using System.Windows.Controls;

namespace RailwayInformationDesk.Views;

public partial class DispatcherWindow : Window
{
    private readonly RailwayInformationDeskContext _context = new();
    private readonly Station _station;

    public DispatcherWindow(Station station)
    {
        InitializeComponent();
        _station = station;
        Title += $" — {_station.Name}";
        LoadIncomingTrains();
    }

    private async void LoadIncomingTrains()
    {
        var now = DateTime.Now;
        var end = now.AddHours(12);
        var dateToday = DateOnly.FromDateTime(now);

        // Загружаем остановки шаблонов для текущей станции
        var stops = await _context.ScheduleTemplateStops
            .Where(ts => ts.StationId == _station.Id && ts.ArrivalTime.HasValue)
            .Include(ts => ts.Template)
                .ThenInclude(t => t.Route)
            .Include(ts => ts.Template)
                .ThenInclude(t => t.TrainType)
            .ToListAsync();

        var viewModels = new List<DispatcherViewModel>();

        foreach (var stop in stops)
        {
            // Находим экземпляр рейса на сегодня
            var tripInstance = await _context.TripInstances
                .FirstOrDefaultAsync(ti => ti.TemplateId == stop.TemplateId && ti.TripDate == dateToday);

            if (tripInstance == null) continue;

            // Планируемое время прибытия
            var plannedArrival = new DateTime(dateToday.Year, dateToday.Month, dateToday.Day,
                stop.ArrivalTime!.Value.Hour, stop.ArrivalTime!.Value.Minute, 0);

            if (plannedArrival < now)
                plannedArrival = plannedArrival.AddDays(1);

            if (plannedArrival > end) continue;

            // Создаём ViewModel БЕЗ загрузки из ActualSchedule
            viewModels.Add(new DispatcherViewModel
            {
                Stop = stop,
                TripInstance = tripInstance,
                ActualSchedule = new ActualSchedule
                {
                    StopId = stop.Id,
                    TripInstanceId = tripInstance.Id,
                    TripInstance = tripInstance,
                    // Id = 0 → будет добавлен в БД только при сохранении с данными
                },
                PlannedArrivalTime = plannedArrival,
                PlannedDepartureTime = stop.DepartureTime.HasValue
                    ? plannedArrival.Date.Add(stop.DepartureTime.Value.ToTimeSpan())
                    : (DateTime?)null
            });
        }

        IncomingTrainsGrid.ItemsSource = viewModels.OrderBy(vm => vm.PlannedArrivalTime).ToList();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewModels = (List<DispatcherViewModel>)IncomingTrainsGrid.ItemsSource;

            foreach (var vm in viewModels)
            {
                // Проверяем, были ли введены фактические данные
                bool hasData = !string.IsNullOrWhiteSpace(vm.ActualPlatform) ||
                               vm.ActualSchedule.ActualArrivalTime.HasValue ||
                               vm.ActualSchedule.ActualDepartureTime.HasValue;

                if (hasData)
                {
                    if (vm.PlannedDepartureTime.HasValue && vm.ActualSchedule.ActualDepartureTime.HasValue)
                    {
                        var planned = vm.PlannedDepartureTime.Value;
                        var actual = vm.ActualSchedule.ActualDepartureTime.Value;

                        if (actual > planned)
                        {
                            // Задержка = разница в минутах
                            vm.ActualSchedule.DelayMinutes = (int)(actual - planned).TotalMinutes;
                        }
                        else
                        {
                            // Поезд отправился вовремя или раньше → задержка = 0
                            vm.ActualSchedule.DelayMinutes = 0;
                        }
                    }
                    else
                    {
                        // Если нет данных — сбрасываем задержку
                        vm.ActualSchedule.DelayMinutes = null;
                    }

                    _context.ActualSchedules.Add(vm.ActualSchedule); 
                }
            }

            await _context.SaveChangesAsync();
            MessageBox.Show("Фактические данные сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _context?.Dispose();
        base.OnClosed(e);
    }

    private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
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
}

public class DispatcherViewModel
{
    public ScheduleTemplateStop Stop { get; set; } = null!;
    public TripInstance TripInstance { get; set; } = null!;
    public ActualSchedule ActualSchedule { get; set; } = null!;
    public DateTime PlannedArrivalTime { get; set; }
    public DateTime? PlannedDepartureTime { get; set; }

    public string? ActualPlatform
    {
        get => ActualSchedule.ActualPlatform;
        set => ActualSchedule.ActualPlatform = value;
    }
}

