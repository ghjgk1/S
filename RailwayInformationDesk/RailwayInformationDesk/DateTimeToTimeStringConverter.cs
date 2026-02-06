// Converters/DateTimeToTimeStringConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace RailwayInformationDesk.Converters;

public class DateTimeToTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DateTime dt ? dt.ToString("HH:mm") : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text) return null;

        // Разрешаем частичный ввод: "9", "91", "912" и т.д.
        var digits = new string(text.Where(char.IsDigit).ToArray());

        if (digits.Length == 0) return null;

        // Формируем временную строку в формате HH:mm
        string timeStr = digits.Length switch
        {
            1 => $"{digits[0]}",
            2 => $"{digits[0..2]}",
            3 => $"{digits[0..2]}:{digits[2]}",
            >= 4 => $"{digits[0..2]}:{digits.Substring(2, 2)}",
            _ => ""
        };

        // Ограничиваем значения
        if (TimeOnly.TryParseExact(timeStr, "HH:mm", culture, DateTimeStyles.None, out var time))
        {
            // Возвращаем DateTime с фиктивной датой (она будет заменена позже)
            return new DateTime(1900, 1, 1, time.Hour, time.Minute, 0);
        }

        return null;
    }
}