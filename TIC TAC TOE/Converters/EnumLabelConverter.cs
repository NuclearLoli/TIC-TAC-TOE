using System;
using System.Globalization;
using System.Windows.Data;
using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.Converters;

public class EnumLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            GameMode.PvP => "PvP",
            GameMode.PvC => "PvC",
            AiDifficulty.De => "Dễ",
            AiDifficulty.Vua => "Vừa",
            AiDifficulty.SieuKho => "Siêu khó",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
