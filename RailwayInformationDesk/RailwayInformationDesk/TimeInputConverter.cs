// Converters/TimeInputConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace RailwayInformationDesk.Converters;

public class TimeInputConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Из модели в UI: формат "HH:mm"
        if (value is DateTime dateTime)
            return dateTime.ToString("HH:mm");
        if (value is string str && TimeOnly.TryParse(str, out var time))
            return time.ToString("HH:mm");
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Из UI в модель: обработка ввода
        if (value is not string input)
            return null;

        // Убираем всё, кроме цифр
        string digits = new string(input.Where(char.IsDigit).ToArray());

        // Автоматически вставляем ':' после 2 цифр
        string formatted = digits.Length switch
        {
            0 => "",
            1 => digits,
            2 => $"{digits}:",
            3 => $"{digits[0..2]}:{digits[2]}",
            4 => $"{digits[0..2]}:{digits[2..4]}",
            _ => $"{digits[0..2]}:{digits[2..4]}" // ограничиваем 4 цифрами
        };

        // Возвращаем отформатированную строку для отображения
        // Но в модель передаём только цифры (или null)
        return digits.Length >= 4 ? formatted : formatted;
    }
}