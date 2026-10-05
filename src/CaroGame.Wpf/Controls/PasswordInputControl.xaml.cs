using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CaroGame.Wpf.Controls;

public partial class PasswordInputControl : UserControl
{
    private bool _isSyncing = false;

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(PasswordInputControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChangedCallback));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(PasswordInputControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPasswordRevealedProperty =
        DependencyProperty.Register(
            nameof(IsPasswordRevealed),
            typeof(bool),
            typeof(PasswordInputControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPasswordRevealedChangedCallback));

    public static readonly DependencyProperty EnterCommandProperty =
        DependencyProperty.Register(
            nameof(EnterCommand),
            typeof(ICommand),
            typeof(PasswordInputControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(PasswordInputControl),
            new PropertyMetadata(new CornerRadius(8)));

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsPasswordRevealed
    {
        get => (bool)GetValue(IsPasswordRevealedProperty);
        set => SetValue(IsPasswordRevealedProperty, value);
    }

    public ICommand? EnterCommand
    {
        get => (ICommand?)GetValue(EnterCommandProperty);
        set => SetValue(EnterCommandProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public PasswordInputControl()
    {
        InitializeComponent();
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1D1A"));
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3C38"));
        BorderThickness = new Thickness(1);
        Height = 42;
        FontSize = 14;
        UpdatePlaceholderVisibility();
    }

    private static void OnPasswordChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordInputControl control && !control._isSyncing)
        {
            string newPassword = (string)e.NewValue ?? string.Empty;
            control._isSyncing = true;
            if (control.InternalPasswordBox.Password != newPassword)
            {
                control.InternalPasswordBox.Password = newPassword;
            }
            if (control.InternalTextBox.Text != newPassword)
            {
                control.InternalTextBox.Text = newPassword;
            }
            control._isSyncing = false;
            control.UpdatePlaceholderVisibility();
        }
    }

    private static void OnIsPasswordRevealedChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordInputControl control)
        {
            control.UpdateVisibilityState((bool)e.NewValue);
        }
    }

    private void InternalPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        Password = InternalPasswordBox.Password;
        InternalTextBox.Text = InternalPasswordBox.Password;
        _isSyncing = false;
        UpdatePlaceholderVisibility();
    }

    private void InternalTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        Password = InternalTextBox.Text;
        InternalPasswordBox.Password = InternalTextBox.Text;
        _isSyncing = false;
        UpdatePlaceholderVisibility();
    }

    private void ToggleVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        IsPasswordRevealed = !IsPasswordRevealed;
    }

    private void UpdateVisibilityState(bool isRevealed)
    {
        if (isRevealed)
        {
            InternalPasswordBox.Visibility = Visibility.Collapsed;
            InternalTextBox.Visibility = Visibility.Visible;
            EyeIconText.Text = "🙈";
            EyeIconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#81B64C"));
            ToggleVisibilityButton.ToolTip = "Ẩn mật khẩu";
            InternalTextBox.Focus();
            InternalTextBox.CaretIndex = InternalTextBox.Text.Length;
        }
        else
        {
            InternalTextBox.Visibility = Visibility.Collapsed;
            InternalPasswordBox.Visibility = Visibility.Visible;
            EyeIconText.Text = "👁️";
            EyeIconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9C99"));
            ToggleVisibilityButton.ToolTip = "Hiện mật khẩu";
            InternalPasswordBox.Focus();
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        if (PlaceholderBlock != null)
        {
            PlaceholderBlock.Visibility = string.IsNullOrEmpty(Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void InputControl_GotFocus(object sender, RoutedEventArgs e)
    {
        OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#81B64C"));
    }

    private void InputControl_LostFocus(object sender, RoutedEventArgs e)
    {
        OuterBorder.BorderBrush = (Brush)GetValue(BorderBrushProperty) ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3C38"));
    }

    private void InternalPasswordBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && EnterCommand != null && EnterCommand.CanExecute(null))
        {
            EnterCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void InternalTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && EnterCommand != null && EnterCommand.CanExecute(null))
        {
            EnterCommand.Execute(null);
            e.Handled = true;
        }
    }
}
