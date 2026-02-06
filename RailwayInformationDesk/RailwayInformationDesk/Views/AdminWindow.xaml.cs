// Views/AdminWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using RailwayInformationDesk.Converters;
using RailwayInformationDesk.Data;
using RailwayInformationDesk.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RailwayInformationDesk.Views;

public partial class AdminWindow : Window
{
    private readonly RailwayInformationDeskContext _context = new();

    public ObservableCollection<Station> Stations { get; } = [];
    public ObservableCollection<Route> Routes { get; } = [];
    public ObservableCollection<TrainType> TrainTypes { get; } = [];
    public ObservableCollection<ScheduleTemplate> Templates { get; } = [];
    public ObservableCollection<ScheduleTemplateStop> TemplateStops { get; } = [];
    public ObservableCollection<TripInstance> TripInstances { get; } = [];
    public ObservableCollection<User> Users { get; } = [];

    private readonly HashSet<int> _existingTemplateIds = new();

    private List<Station> _allStations = [];
    private List<Route> _allRoutes = [];
    private List<TrainType> _allTrainTypes = [];
    private List<ScheduleTemplate> _allTemplates = [];
    private List<ScheduleTemplateStop> _allTemplateStops = [];
    private List<TripInstance> _allTripInstances = [];
    private List<User> _allUsers = [];

    public List<string> Roles { get; } = ["Администратор", "Дежурный"];

    public AdminWindow()
    {
        InitializeComponent();
        DataContext = this;
        _ = LoadDataAsync();
        UsersGrid.AddingNewItem += UsersGrid_AddingNewItem;
    }

    private void UsersGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
        e.NewItem = new User
        {
            Login = "",
            PasswordHash = "",
            FullName = "",
            Role = "Администратор",
            StationId = null
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var stations = await _context.Stations.OrderBy(s => s.Id).ToListAsync();
            Stations.Clear();
            foreach (var stat in stations) 
                Stations.Add(stat);
            _allStations = [.. stations];

            var trainTypes = await _context.TrainTypes.OrderBy(t => t.Id).ToListAsync();
            TrainTypes.Clear();
            foreach(var trainType in trainTypes)
                TrainTypes.Add(trainType);
            _allTrainTypes = [.. trainTypes];

            var routes = await _context.Routes
                .Include(r => r.DepartureStation)
                .Include(r => r.ArrivalStation)
                .OrderBy(r => r.Id)
                .ToListAsync();
            Routes.Clear(); 
            foreach (var route in routes)
                Routes.Add(route);
            _allRoutes = [.. routes];

            var templates = await _context.ScheduleTemplates
                .Include(t => t.Route)
                .Include(t => t.TrainType)
                .OrderBy(t => t.Id)
                .ToListAsync();
            Templates.Clear();
            foreach (var template in templates)
                Templates.Add(template);
            _allTemplates = [.. templates];

            _existingTemplateIds.Clear();
            _existingTemplateIds.UnionWith(templates.Select(t => t.Id));

            var templateStops = await _context.ScheduleTemplateStops
                .Include(ts => ts.Station)
                .Include(ts => ts.Template)
                .OrderBy(ts => ts.Id)
                .ToListAsync();
            TemplateStops.Clear();   
            foreach (var templateStop in templateStops)
                TemplateStops.Add(templateStop);
            _allTemplateStops = [.. templateStops];

            var tripInstances = await _context.TripInstances
                .Include(ti => ti.Template)
                .ThenInclude(t => t.Route)
                .OrderBy(ti => ti.Id)
                .ToListAsync();
            TripInstances.Clear(); 
            foreach(var tripInstance in tripInstances)
                TripInstances.Add(tripInstance);
            _allTripInstances = [.. tripInstances];

            var users = await _context.Users
                .Include(u => u.Station)
                .OrderBy(u => u.Id)
                .ToListAsync();
            Users.Clear(); 
            foreach( var user in users)
                Users.Add(user);
            _allUsers = [.. users];

            // Обновляем конвертеры
            IdToNameConverter.Stations = Stations;
            IdToNameConverter.Routes = Routes;
            IdToNameConverter.TrainTypes = TrainTypes;
            IdToNameConverter.Templates = Templates;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DepartureOrArrivalStation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.DataContext is not Route route) return;

        var dep = Stations.FirstOrDefault(s => s.Id == route.DepartureStationId);
        var arr = Stations.FirstOrDefault(s => s.Id == route.ArrivalStationId);
        if (dep != null && arr != null)
        {
            string name = $"{dep.Name} > {arr.Name}";
            if (route.Name == name) return;
            if (MessageBox.Show($"Обновить название маршрута на:\n\"{name}\"?", "Предложение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                route.Name = name;
        }
    }

    private async void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || sender is not DataGrid dg || dg.SelectedItem == null) return;

        if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            e.Handled = true;
            return;
        }

        bool deleted = false;

        switch (dg.Name)
        {
            case "StationsGrid":
                if (dg.SelectedItem is Station s)
                {
                    if (Routes.Any(r => r.DepartureStationId == s.Id || r.ArrivalStationId == s.Id) ||
                        Users.Any(u => u.StationId == s.Id) ||
                        TemplateStops.Any(ts => ts.StationId == s.Id))
                    {
                        MessageBox.Show("Нельзя удалить: станция используется.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Handled = true;
                        return;
                    }
                    Stations.Remove(s);
                    _context.Stations.Remove(s);
                    deleted = true;
                }
                break;
            case "RoutesGrid":
                if (dg.SelectedItem is Route r)
                {
                    if (Templates.Any(t => t.RouteId == r.Id))
                    {
                        MessageBox.Show("Нельзя удалить маршрут: есть связанные шаблоны.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Handled = true;
                        return;
                    }
                    Routes.Remove(r);
                    _context.Routes.Remove(r);
                    deleted = true;
                }
                break;
            case "TrainTypesGrid":
                if (dg.SelectedItem is TrainType tt)
                {
                    if (Templates.Any(t => t.TrainTypeId == tt.Id))
                    {
                        MessageBox.Show("Нельзя удалить тип: есть связанные шаблоны.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Handled = true;
                        return;
                    }
                    TrainTypes.Remove(tt);
                    _context.TrainTypes.Remove(tt);
                    deleted = true;
                }
                break;
            case "TemplatesGrid":
                if (dg.SelectedItem is ScheduleTemplate t)
                {
                    var stopsToRemove = TemplateStops.Where(ts => ts.TemplateId == t.Id).ToList();
                    foreach (var stop in stopsToRemove)
                    {
                        TemplateStops.Remove(stop);
                        _context.ScheduleTemplateStops.Remove(stop);
                    }
                    var tripsToRemove = TripInstances.Where(ti => ti.TemplateId == t.Id).ToList();
                    foreach (var trip in tripsToRemove)
                    {
                        TripInstances.Remove(trip);
                        _context.TripInstances.Remove(trip);
                    }
                    Templates.Remove(t);
                    _context.ScheduleTemplates.Remove(t);
                    deleted = true;
                }
                break;
            case "TemplateStopsGrid":
                if (dg.SelectedItem is ScheduleTemplateStop ts)
                {
                    TemplateStops.Remove(ts);
                    _context.ScheduleTemplateStops.Remove(ts);
                    deleted = true;
                }
                break;
            case "TripInstancesGrid":
                if (dg.SelectedItem is TripInstance ti)
                {
                    TripInstances.Remove(ti);
                    _context.TripInstances.Remove(ti);
                    deleted = true;
                }
                break;
            case "UsersGrid":
                if (dg.SelectedItem is User u)
                {
                    Users.Remove(u);
                    _context.Users.Remove(u);
                    deleted = true;
                }
                break;
        }

        if (deleted)
        {
            try
            {
                await _context.SaveChangesAsync();
                RefreshAllLists(); // Обновить списки после удаления
                MessageBox.Show("Запись удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        e.Handled = true;
    }

    private string GetCurrentTabHeader() =>
        MainTabControl.SelectedItem is TabItem item ? item.Header.ToString()! : "";

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = SearchBox.Text.Trim().ToLower();
        switch (GetCurrentTabHeader())
        {
            case "Станции": Filter(filter, Stations, _allStations, s => s.Name.Contains(filter) || s.City.Contains(filter) || s.Region.Contains(filter)); break;
            case "Маршруты": Filter(filter, Routes, _allRoutes, r => r.Name.Contains(filter) || GetStationName(r.DepartureStationId).Contains(filter) || GetStationName(r.ArrivalStationId).Contains(filter)); break;
            case "Типы поездов": Filter(filter, TrainTypes, _allTrainTypes, t => t.Name.Contains(filter)); break;
            case "Шаблоны расписаний": Filter(filter, Templates, _allTemplates, t => GetRouteName(t.RouteId).Contains(filter) || GetTrainTypeName(t.TrainTypeId).Contains(filter)); break;
            case "Остановки в шаблонах": Filter(filter, TemplateStops, _allTemplateStops, ts => GetStationName(ts.StationId).Contains(filter) || GetTemplateName(ts.TemplateId).Contains(filter)); break;
            case "Экземпляры рейсов": Filter(filter, TripInstances, _allTripInstances, ti => GetTemplateName(ti.TemplateId).Contains(filter) || ti.TripDate.ToString().Contains(filter)); break;
            case "Пользователи": Filter(filter, Users, _allUsers, u => u.Login.Contains(filter) || u.FullName.Contains(filter) || u.Role.Contains(filter) || GetStationName(u.StationId).Contains(filter)); break;
        }
    }

    private string GetStationName(int? id) => Stations.FirstOrDefault(s => s.Id == id)?.Name ?? "";
    private string GetRouteName(int id) => Routes.FirstOrDefault(r => r.Id == id)?.Name ?? "";
    private string GetTrainTypeName(int id) => TrainTypes.FirstOrDefault(t => t.Id == id)?.Name ?? "";
    private string GetTemplateName(int id) => Templates.FirstOrDefault(t => t.Id == id)?.Route.Name ?? "";

    private void Filter<T>(string filter, ObservableCollection<T> target, List<T> source, Func<T, bool> predicate)
    {
        target.Clear();
        var items = string.IsNullOrEmpty(filter) ? source : source.Where(predicate).ToList();
        foreach (var item in items) target.Add(item);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateUsers();
            ValidateTemplates();
            ValidateTripInstances();

            // Этап 1: Добавляем новые сущности (без остановок!)
            AddNewEntitiesStage();
            await _context.SaveChangesAsync(); // ← шаблоны получают Id

            // Этап 2: Определяем, какие шаблоны были созданы в этом сеансе
            var newlySavedTemplates = Templates
                .Where(t => t.Id > 0 && !_existingTemplateIds.Contains(t.Id))
                .ToList();

            foreach (var template in newlySavedTemplates)
            {
                // Создаём базовые остановки ТОЛЬКО для новых шаблонов
                var route = Routes.FirstOrDefault(r => r.Id == template.RouteId);
                if (route == null)
                    throw new InvalidOperationException($"Маршрут для шаблона ID={template.Id} не найден.");

                var firstStop = new ScheduleTemplateStop
                {
                    TemplateId = template.Id,
                    StationId = route.DepartureStationId,
                    StopOrder = 0,
                    DepartureTime = new TimeOnly(8, 0),
                    Platform = "1"
                };

                var lastStop = new ScheduleTemplateStop
                {
                    TemplateId = template.Id,
                    StationId = route.ArrivalStationId,
                    StopOrder = 1,
                    ArrivalTime = new TimeOnly(20, 0),
                    Platform = "1"
                };

                TemplateStops.Add(firstStop);
                TemplateStops.Add(lastStop);
                _context.ScheduleTemplateStops.Add(firstStop);
                _context.ScheduleTemplateStops.Add(lastStop);
            }

            if (newlySavedTemplates.Any())
            {
                await _context.SaveChangesAsync(); 
            }

            RefreshAllLists();
            MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool IsTripAllowedOnDate(ScheduleTemplate template, DateOnly tripDate)
    {
        if (string.IsNullOrEmpty(template.DaysOfWeek) || template.DaysOfWeek.Length != 7)
            return false;

        // DayOfWeek в .NET: Monday=1, Tuesday=2, ..., Sunday=0
        int dayIndex = tripDate.DayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(tripDate))
        };

        return template.DaysOfWeek[dayIndex] == '1';
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

    private void ValidateUsers()
    {
        foreach (var u in Users)
        {
            if (string.IsNullOrWhiteSpace(u.Login)) throw new InvalidOperationException("Логин не может быть пустым.");
            if (string.IsNullOrWhiteSpace(u.PasswordHash)) throw new InvalidOperationException($"Пароль не указан для '{u.Login}'.");
            if (string.IsNullOrWhiteSpace(u.FullName)) throw new InvalidOperationException($"ФИО не указано для '{u.Login}'.");
            if (u.Role != "Администратор" && u.Role != "Дежурный")
                throw new InvalidOperationException($"Недопустимая роль: '{u.Role}'.");
            if (u.Role == "Дежурный" && !u.StationId.HasValue)
                throw new InvalidOperationException($"У дежурного '{u.Login}' не указана станция.");
            if (u.Role == "Администратор") u.StationId = null;
        }
    }

    private void AddNewEntitiesStage()
    {
        if (Stations.Any(s => s.Id == 0)) _context.Stations.AddRange(Stations.Where(s => s.Id == 0));
        if (Routes.Any(r => r.Id == 0)) _context.Routes.AddRange(Routes.Where(r => r.Id == 0));
        if (TrainTypes.Any(t => t.Id == 0)) _context.TrainTypes.AddRange(TrainTypes.Where(t => t.Id == 0));
        if (Templates.Any(t => t.Id == 0)) _context.ScheduleTemplates.AddRange(Templates.Where(t => t.Id == 0));
        if (TripInstances.Any(ti => ti.Id == 0)) _context.TripInstances.AddRange(TripInstances.Where(ti => ti.Id == 0));
        if (Users.Any(u => u.Id == 0)) _context.Users.AddRange(Users.Where(u => u.Id == 0));
    }

    private void RefreshAllLists()
    {
        _allStations = [.. Stations];
        _allRoutes = [.. Routes];
        _allTrainTypes = [.. TrainTypes];
        _allTemplates = [.. Templates];
        _allTemplateStops = [.. TemplateStops];
        _allTripInstances = [.. TripInstances];
        _allUsers = [.. Users];
    }

    private void ValidateTemplates()
    {
        foreach (var template in Templates)
        {
            var days = template.DaysOfWeek?.Trim();

            if (string.IsNullOrEmpty(days))
                throw new InvalidOperationException($"У шаблона (ID={template.Id}) не указаны дни недели.");

            if (days.Length != 7)
                throw new InvalidOperationException($"Дни недели для шаблона (ID={template.Id}) должны содержать ровно 7 символов (например: 1100101).");

            if (!days.All(c => c == '0' || c == '1'))
                throw new InvalidOperationException($"Дни недели для шаблона (ID={template.Id}) могут содержать только '0' и '1'.");
        }
    }

    private void ValidateTripInstances()
    {
        foreach (var trip in TripInstances.Where(ti => ti.Id == 0))
        {
            var template = Templates.FirstOrDefault(t => t.Id == trip.TemplateId);
            if (template == null)
                throw new InvalidOperationException($"Шаблон для рейса не найден (TemplateId={trip.TemplateId}).");

            if (!IsTripAllowedOnDate(template, trip.TripDate))
            {
                var dayName = trip.TripDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => "понедельник",
                    DayOfWeek.Tuesday => "вторник",
                    DayOfWeek.Wednesday => "среда",
                    DayOfWeek.Thursday => "четверг",
                    DayOfWeek.Friday => "пятница",
                    DayOfWeek.Saturday => "суббота",
                    DayOfWeek.Sunday => "воскресенье",
                    _ => "неизвестный день"
                };
                throw new InvalidOperationException(
                    $"Нельзя создать рейс на {trip.TripDate:d} ({dayName}) — поезд по шаблону \"{template.Route.Name}\" не ходит в этот день.");
            }
        }
    }

    private void TemplatesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TemplatesGrid.SelectedItem is not ScheduleTemplate template) return;

        if (template.Id == 0) return;

        var stopsWindow = new TemplateStopsWindow(template, _context);
        stopsWindow.ShowDialog();

        // После закрытия — перезагрузим остановки, если нужно
        _ = LoadDataAsync(); // или частичная перезагрузка только TemplateStops
    }
}