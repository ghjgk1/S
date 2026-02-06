// Views/LoginWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using RailwayInformationDesk.Data;
using RailwayInformationDesk.Views;
using System.Windows;

namespace RailwayInformationDesk.Views;

public partial class LoginWindow : Window
{
    private readonly RailwayInformationDeskContext _context;

    public LoginWindow()
    {
        InitializeComponent();
        _context = new RailwayInformationDeskContext();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string login = LoginBox.Text?.Trim() ?? "";
            string password = PasswordBox.Password ?? "";

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = await _context.Users
                .Include(u => u.Station)
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
            {
                MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Сравнение без хэширования (пароль в открытом виде)
            if (password != user.PasswordHash)
            {
                MessageBox.Show("Неверный пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Открытие нужного окна в зависимости от роли
            switch (user.Role)
            {
                case "Администратор":
                    new AdminWindow().Show();
                    break;
                case "Дежурный":
                    if (user.Station == null)
                    {
                        MessageBox.Show("У дежурного не указана станция", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    new DispatcherWindow(user.Station).Show();
                    break;
                case "Пользователь":
                    new UserSearchWindow().Show();
                    break;
                default:
                    MessageBox.Show("Неизвестная роль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            this.Close();
        }
        catch { }
    }

        private void LoginUserButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            new UserSearchWindow().Show();
            this.Close();
        }
        catch { }
    }

    
}