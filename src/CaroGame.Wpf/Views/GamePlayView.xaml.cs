using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CaroGame.Wpf.ViewModels;

namespace CaroGame.Wpf.Views;

public partial class GamePlayView : UserControl
{
    private INotifyCollectionChanged? _currentChatMessages;

    public GamePlayView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_currentChatMessages != null)
        {
            _currentChatMessages.CollectionChanged -= OnChatMessagesChanged;
            _currentChatMessages = null;
        }

        if (e.NewValue is GamePlayViewModel vm)
        {
            _currentChatMessages = vm.ChatMessages;
            _currentChatMessages.CollectionChanged += OnChatMessagesChanged;
        }
    }

    private void OnChatMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }, DispatcherPriority.Loaded);
        }
    }
}
