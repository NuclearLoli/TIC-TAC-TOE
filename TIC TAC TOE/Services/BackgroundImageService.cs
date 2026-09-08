using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TIC_TAC_TOE.Services;

public class BackgroundImageService
{
    public string? PickImageFile()
    {
        OpenFileDialog dialog = new()
        {
            Title = "Chon anh nen",
            Filter = "Image Files|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public Brush CreateBackgroundBrush(string filePath)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath);
        bitmap.EndInit();
        bitmap.Freeze();

        ImageBrush brush = new(bitmap)
        {
            Stretch = Stretch.UniformToFill,
            Opacity = 0.62
        };

        brush.Freeze();
        return brush;
    }

    public string GetDisplayName(string? filePath)
    {
        return string.IsNullOrWhiteSpace(filePath)
            ? "Nền mặc định"
            : Path.GetFileName(filePath);
    }
}
