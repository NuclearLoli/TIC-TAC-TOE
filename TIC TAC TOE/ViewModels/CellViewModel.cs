using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TIC_TAC_TOE.Commands;
using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.ViewModels;

public class CellViewModel : ViewModelBase
{
    private readonly RelayCommand _playCommand;
    private readonly Action<int, int> _playAction;
    private string _displayText = string.Empty;
    private bool _isAvailable;
    private Brush _foregroundBrush = Brushes.Transparent;
    private Brush _backgroundBrush = new SolidColorBrush(Color.FromArgb(82, 249, 241, 226));
    private Brush _borderBrush = new SolidColorBrush(Color.FromArgb(168, 76, 60, 46));
    private Brush _hintBrush = new SolidColorBrush(Color.FromArgb(200, 98, 77, 58));
    private Visibility _hintVisibility = Visibility.Collapsed;

    public CellViewModel(int row, int column, Action<int, int> playAction)
    {
        Row = row;
        Column = column;
        _playAction = playAction;
        _playCommand = new RelayCommand(Play, () => IsAvailable);
    }

    public int Row { get; }

    public int Column { get; }

    public string DisplayText
    {
        get => _displayText;
        private set => SetProperty(ref _displayText, value);
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        private set => SetProperty(ref _isAvailable, value);
    }

    public Brush ForegroundBrush
    {
        get => _foregroundBrush;
        private set => SetProperty(ref _foregroundBrush, value);
    }

    public Brush BackgroundBrush
    {
        get => _backgroundBrush;
        private set => SetProperty(ref _backgroundBrush, value);
    }

    public Brush BorderBrush
    {
        get => _borderBrush;
        private set => SetProperty(ref _borderBrush, value);
    }

    public Brush HintBrush
    {
        get => _hintBrush;
        private set => SetProperty(ref _hintBrush, value);
    }

    public Visibility HintVisibility
    {
        get => _hintVisibility;
        private set => SetProperty(ref _hintVisibility, value);
    }

    public ICommand PlayCommand => _playCommand;

    public void RefreshState(CellState cellState, bool isLegalTarget)
    {
        DisplayText = cellState switch
        {
            CellState.PlayerX => "X",
            CellState.PlayerO => "O",
            _ => string.Empty
        };

        if (cellState == CellState.PlayerX)
        {
            ForegroundBrush = new SolidColorBrush(Color.FromRgb(178, 42, 54));
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(238, 255, 245, 235));
            BorderBrush = new SolidColorBrush(Color.FromRgb(91, 59, 40));
            HintVisibility = Visibility.Collapsed;
        }
        else if (cellState == CellState.PlayerO)
        {
            ForegroundBrush = new SolidColorBrush(Color.FromRgb(28, 77, 148));
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(238, 255, 245, 235));
            BorderBrush = new SolidColorBrush(Color.FromRgb(91, 59, 40));
            HintVisibility = Visibility.Collapsed;
        }
        else if (isLegalTarget)
        {
            ForegroundBrush = Brushes.Transparent;
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(188, 255, 248, 232));
            BorderBrush = new SolidColorBrush(Color.FromRgb(116, 83, 54));
            HintBrush = new SolidColorBrush(Color.FromArgb(210, 107, 82, 57));
            HintVisibility = Visibility.Visible;
        }
        else
        {
            ForegroundBrush = Brushes.Transparent;
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(92, 227, 213, 189));
            BorderBrush = new SolidColorBrush(Color.FromArgb(142, 82, 66, 53));
            HintVisibility = Visibility.Collapsed;
        }

        IsAvailable = isLegalTarget && cellState == CellState.Empty;
        _playCommand.RaiseCanExecuteChanged();
    }

    private void Play()
    {
        _playAction(Row, Column);
    }
}
