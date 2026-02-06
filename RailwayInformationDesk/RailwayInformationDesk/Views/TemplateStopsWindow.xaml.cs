// Views/TemplateStopsWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using RailwayInformationDesk.Converters;
using RailwayInformationDesk.Data;
using RailwayInformationDesk.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RailwayInformationDesk.Views;

public partial class TemplateStopsWindow : Window
{
    private readonly ScheduleTemplate _template;
    private readonly RailwayInformationDeskContext _context;
    public ObservableCollection<ScheduleTemplateStop> Stops { get; } = [];
    public List<Station> AllStations { get; private set; } = [];

    // Кэшируем ID первой и последней станции из маршрута
    private int _departureStationId;
    private int _arrivalStationId;

    public TemplateStopsWindow(ScheduleTemplate template, RailwayInformationDeskContext context)
    {
        InitializeComponent();
        _template = template;
        _context = context;
        DataContext = this;

        // Получаем станции маршрута
        _departureStationId = template.Route.DepartureStationId;
        _arrivalStationId = template.Route.ArrivalStationId;

        Title = $"Остановки расписания: {template.Route.Name}";
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var stops = await _context.ScheduleTemplateStops
            .Where(s => s.TemplateId == _template.Id)
            .OrderBy(s => s.StopOrder)
            .ToListAsync();

        // Если остановок нет — создаём начальную и конечную
        if (!stops.Any())
        {
            stops.Add(new ScheduleTemplateStop
            {
                StationId = _departureStationId,
                StopOrder = 0,
                DepartureTime = new TimeOnly(8, 0),
                Platform = "1"
            });
            stops.Add(new ScheduleTemplateStop
            {
                StationId = _arrivalStationId,
                StopOrder = 1,
                ArrivalTime = new TimeOnly(18, 0),
                Platform = "1"
            });
        }

        Stops.Clear();
        foreach (var s in stops) Stops.Add(s);

        AllStations = await _context.Stations.OrderBy(s => s.Name).ToListAsync();
        IdToNameConverter.Stations = new(AllStations);
    }

    private void StopsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && sender is DataGrid dg && dg.SelectedItem is ScheduleTemplateStop stop)
        {
            // Запрещаем удаление первой и последней
            if (IsTerminalStop(stop))
            {
                MessageBox.Show("Нельзя удалить первую или последнюю остановку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            if (MessageBox.Show("Удалить остановку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Stops.Remove(stop);
                _context.ScheduleTemplateStops.Remove(stop);
                e.Handled = true;
            }
        }
    }

    private bool IsTerminalStop(ScheduleTemplateStop stop)
    {
        return (stop.StationId == _departureStationId && stop.StopOrder == Stops.Min(s => s.StopOrder)) ||
               (stop.StationId == _arrivalStationId && stop.StopOrder == Stops.Max(s => s.StopOrder));
    }

    private void StopsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is not ScheduleTemplateStop stop) return;

        var isFirst = stop.StationId == _departureStationId && stop.StopOrder == Stops.Min(s => s.StopOrder);
        var isLast = stop.StationId == _arrivalStationId && stop.StopOrder == Stops.Max(s => s.StopOrder);

        if (isFirst || isLast)
        {
            // Определяем, какую колонку пытаются редактировать
            if (e.Column.Header?.ToString() == "Станция" || e.Column.Header?.ToString() == "Порядок")
            {
                MessageBox.Show("Первая и последняя остановки определяются маршрутом и не подлежат редактированию.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
            }
        }
    }

    private void StopsGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
        if (Stops.Count < 2)
        {
            MessageBox.Show("Сначала сохраните базовые остановки (начало и конец).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Находим последнюю остановку (максимальный StopOrder)
        var lastStop = Stops.OrderByDescending(s => s.StopOrder).First();

        // Новая остановка получает порядок = текущий максимум
        int newOrder = lastStop.StopOrder;

        // Сдвигаем последнюю на +1
        lastStop.StopOrder++;

        // Создаём новую остановку **перед** бывшей последней
        e.NewItem = new ScheduleTemplateStop
        {
            TemplateId = _template.Id,
            StationId = AllStations
                .FirstOrDefault(s => s.Id != _departureStationId && s.Id != _arrivalStationId)?.Id
                ?? AllStations.First().Id,
            StopOrder = newOrder,
            Platform = "1"
        };
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateStops();
            // Валидация: проверка уникальности StopOrder и границ
            var orders = Stops.Select(s => s.StopOrder).ToList();
            var minOrder = orders.Min();
            var maxOrder = orders.Max();

            // Первая и последняя должны быть фиксированы
            var first = Stops.First(s => s.StopOrder == minOrder);
            var last = Stops.First(s => s.StopOrder == maxOrder);

            if (first.StationId != _departureStationId)
                throw new InvalidOperationException("Первая остановка должна совпадать со станцией отправления маршрута.");
            if (last.StationId != _arrivalStationId)
                throw new InvalidOperationException("Последняя остановка должна совпадать со станцией назначения маршрута.");

            // Проверка дубликатов
            if (orders.Count != orders.Distinct().Count())
                throw new InvalidOperationException("Порядок остановок не должен повторяться.");

            // Проверка диапазона: все промежуточные должны быть строго между min и max
            foreach (var stop in Stops)
            {
                if (stop.StopOrder <= minOrder && stop.StationId != _departureStationId)
                    throw new InvalidOperationException("Промежуточная остановка не может иметь порядок ≤ первой.");
                if (stop.StopOrder >= maxOrder && stop.StationId != _arrivalStationId)
                    throw new InvalidOperationException("Промежуточная остановка не может иметь порядок ≥последней.");
            }

            // Присваиваем TemplateId новым
            foreach (var stop in Stops.Where(s => s.Id == 0))
                stop.TemplateId = _template.Id;

            // Синхронизация с БД
            var existingInDb = await _context.ScheduleTemplateStops
                .Where(s => s.TemplateId == _template.Id)
                .ToListAsync();

            foreach (var dbStop in existingInDb)
            {
                if (!Stops.Contains(dbStop))
                    _context.ScheduleTemplateStops.Remove(dbStop);
            }

            var newStops = Stops.Where(s => s.Id == 0).ToList();
            if (newStops.Any())
                _context.ScheduleTemplateStops.AddRange(newStops);

            await _context.SaveChangesAsync();

            MessageBox.Show("Остановки успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void ValidateStops()
    {
        if (Stops.Count < 2)
            throw new InvalidOperationException("Должны быть хотя бы две остановки.");

        var ordered = Stops.OrderBy(s => s.StopOrder).ToList();
        var first = ordered.First();
        var last = ordered.Last();

        if (first.StationId != _departureStationId)
            throw new InvalidOperationException("Первая остановка должна совпадать со станцией отправления маршрута.");
        if (last.StationId != _arrivalStationId)
            throw new InvalidOperationException("Последняя остановка должна совпадать со станцией назначения маршрута.");
        if (first.StopOrder != 0)
            throw new InvalidOperationException("Порядок первой остановки должен быть 0.");
    }
}