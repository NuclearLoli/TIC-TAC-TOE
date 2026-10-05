using System.Windows;

namespace CaroGame.Wpf.Services;

public interface IDialogService
{
    void ShowInformation(string title, string message);
    bool ShowConfirmation(string title, string message);
    string? ShowOpenFileDialog(string title, string filter);
}

public class DialogService : IDialogService
{
    public void ShowInformation(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool ShowConfirmation(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public string? ShowOpenFileDialog(string title, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
