using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace CaroGame.Wpf.Helpers;

public static class ImageHelper
{
    private static readonly ConcurrentDictionary<string, ImageSource?> _imageCache = new();

    public static string? PickAndProcessAvatar(int targetSize = 256, int quality = 85)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện của bạn",
                Filter = "Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tất cả tệp (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true) return null;

            byte[] fileBytes = File.ReadAllBytes(dialog.FileName);
            using var inMs = new MemoryStream(fileBytes);

            var decoder = BitmapDecoder.Create(inMs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) return null;

            var frame = decoder.Frames[0];
            int width = frame.PixelWidth;
            int height = frame.PixelHeight;

            int minDim = Math.Min(width, height);
            int cropX = Math.Max(0, (width - minDim) / 2);
            int cropY = Math.Max(0, (height - minDim) / 2);

            var cropped = new CroppedBitmap(frame, new Int32Rect(cropX, cropY, minDim, minDim));

            BitmapSource finalSource;
            if (minDim > targetSize)
            {
                double scale = (double)targetSize / minDim;
                finalSource = new TransformedBitmap(cropped, new ScaleTransform(scale, scale));
            }
            else
            {
                finalSource = cropped;
            }

            var encoder = new JpegBitmapEncoder { QualityLevel = quality };
            encoder.Frames.Add(BitmapFrame.Create(finalSource));

            using var outMs = new MemoryStream();
            encoder.Save(outMs);
            byte[] jpegBytes = outMs.ToArray();

            string dataUri = $"data:image/jpeg;base64,{Convert.ToBase64String(jpegBytes)}";

            // Cache immediately for snappy UI
            if (finalSource.CanFreeze) finalSource.Freeze();
            _imageCache[dataUri] = finalSource;

            return dataUri;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static ImageSource? GetBitmapFromAvatar(string? avatar)
    {
        if (string.IsNullOrWhiteSpace(avatar)) return null;

        if (_imageCache.TryGetValue(avatar, out var cached))
        {
            return cached;
        }

        try
        {
            if (avatar.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                int commaIdx = avatar.IndexOf(',');
                if (commaIdx >= 0)
                {
                    string base64 = avatar[(commaIdx + 1)..];
                    byte[] bytes = Convert.FromBase64String(base64);

                    using var ms = new MemoryStream(bytes);
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();

                    _imageCache[avatar] = bi;
                    return bi;
                }
            }
            else if (avatar.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     avatar.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(avatar);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                _imageCache[avatar] = bi;
                return bi;
            }
            else if (File.Exists(avatar))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(avatar, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                _imageCache[avatar] = bi;
                return bi;
            }
        }
        catch
        {
            // Invalid image data, fall back to null
        }

        _imageCache[avatar] = null;
        return null;
    }
}
