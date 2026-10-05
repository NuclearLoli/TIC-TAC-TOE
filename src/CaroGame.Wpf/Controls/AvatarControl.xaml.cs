using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CaroGame.Wpf.Helpers;

namespace CaroGame.Wpf.Controls;

public partial class AvatarControl : UserControl
{
    public static readonly DependencyProperty AvatarProperty =
        DependencyProperty.Register(
            nameof(Avatar),
            typeof(string),
            typeof(AvatarControl),
            new PropertyMetadata(string.Empty, OnAvatarOrIconChanged));

    public static readonly DependencyProperty AvatarIconProperty =
        DependencyProperty.Register(
            nameof(AvatarIcon),
            typeof(string),
            typeof(AvatarControl),
            new PropertyMetadata(string.Empty, OnAvatarOrIconChanged));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(double),
            typeof(AvatarControl),
            new PropertyMetadata(40.0));

    public static readonly DependencyProperty DisplayIconProperty =
        DependencyProperty.Register(
            nameof(DisplayIcon),
            typeof(string),
            typeof(AvatarControl),
            new PropertyMetadata("👑"));

    public string Avatar
    {
        get => (string)GetValue(AvatarProperty);
        set => SetValue(AvatarProperty, value);
    }

    public string AvatarIcon
    {
        get => (string)GetValue(AvatarIconProperty);
        set => SetValue(AvatarIconProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public string DisplayIcon
    {
        get => (string)GetValue(DisplayIconProperty);
        set => SetValue(DisplayIconProperty, value);
    }

    public AvatarControl()
    {
        InitializeComponent();
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#453E3B"));
        BorderThickness = new Thickness(1.5);
        FontSize = 18;
        Loaded += (s, e) => UpdateVisual();
        UpdateVisual();
    }

    private static void OnAvatarOrIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AvatarControl control)
        {
            control.UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        string? avatar = Avatar;
        var imageSource = ImageHelper.GetBitmapFromAvatar(avatar);

        if (imageSource != null)
        {
            AvatarBrush.ImageSource = imageSource;
            ImageEllipse.Visibility = Visibility.Visible;
            EmojiBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            ImageEllipse.Visibility = Visibility.Collapsed;
            EmojiBorder.Visibility = Visibility.Visible;

            string icon;
            if (!string.IsNullOrEmpty(AvatarIcon) && AvatarIcon != "👤")
            {
                icon = AvatarIcon;
            }
            else
            {
                icon = (avatar?.ToLowerInvariant()) switch
                {
                    "knight" => "⚔️",
                    "ninja" => "🥷",
                    "bot" => "🤖",
                    "fox" => "🦊",
                    "cat" => "🐱",
                    "dragon" => "🐉",
                    "lightning" => "⚡",
                    "shield" => "🛡️",
                    "star" => "🌟",
                    "king" => "👑",
                    _ => (!string.IsNullOrEmpty(AvatarIcon) ? AvatarIcon : "👑")
                };
            }

            DisplayIcon = icon;
            if (EmojiText != null)
            {
                EmojiText.Text = icon;
            }
        }
    }
}
