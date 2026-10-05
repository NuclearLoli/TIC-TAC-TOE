using System.Globalization;
using System.Windows.Data;
using CaroGame.Core.Enums;

namespace CaroGame.Wpf.Converters;

public class EnumToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            GameMode.PvC => "🤖 Người đấu với Robot (PvC)",
            GameMode.PvP => "👥 2 Người chơi (PvP)",
            AiDifficulty.Easy => "🌱 Dễ (Tập sự)",
            AiDifficulty.Medium => "⚡ Vừa (Cân não)",
            AiDifficulty.Hard => "🔥 Khó (Cao thủ Minimax)",
            RuleType.BlockedBothEnds => "🇻🇳 Luật Việt Nam (Chặn 2 đầu không thắng)",
            RuleType.FreeRule => "🌐 Luật Tự Do (Cứ đủ 5 con là thắng)",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
