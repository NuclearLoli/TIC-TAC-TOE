using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Wpf.Controls;

/// <summary>
/// Bàn cờ Caro vô hạn & tự co giãn hiệu năng cao sử dụng DrawingVisual.
/// Render hàng ngàn ô cờ với 60-120 FPS, hỗ trợ Animations (Pop-in quân cờ, sóng năng lượng ripple, tia sét chiến thắng, camera glide).
/// </summary>
public class InfiniteCaroCanvas : FrameworkElement
{
    public static readonly DependencyProperty BoardProperty =
        DependencyProperty.Register(
            nameof(Board),
            typeof(DynamicBoard),
            typeof(InfiniteCaroCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBoardChanged));

    public static readonly DependencyProperty LastMoveProperty =
        DependencyProperty.Register(
            nameof(LastMove),
            typeof(Coordinate?),
            typeof(InfiniteCaroCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnLastMoveChanged));

    public static readonly DependencyProperty WinningLineProperty =
        DependencyProperty.Register(
            nameof(WinningLine),
            typeof(IReadOnlyList<Coordinate>),
            typeof(InfiniteCaroCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnWinningLineChanged));

    public static readonly DependencyProperty CellClickedCommandProperty =
        DependencyProperty.Register(
            nameof(CellClickedCommand),
            typeof(ICommand),
            typeof(InfiniteCaroCanvas));

    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(
            nameof(IsInteractive),
            typeof(bool),
            typeof(InfiniteCaroCanvas),
            new FrameworkPropertyMetadata(true));

    public DynamicBoard? Board
    {
        get => (DynamicBoard?)GetValue(BoardProperty);
        set => SetValue(BoardProperty, value);
    }

    public Coordinate? LastMove
    {
        get => (Coordinate?)GetValue(LastMoveProperty);
        set => SetValue(LastMoveProperty, value);
    }

    public IReadOnlyList<Coordinate>? WinningLine
    {
        get => (IReadOnlyList<Coordinate>?)GetValue(WinningLineProperty);
        set => SetValue(WinningLineProperty, value);
    }

    public ICommand? CellClickedCommand
    {
        get => (ICommand?)GetValue(CellClickedCommandProperty);
        set => SetValue(CellClickedCommandProperty, value);
    }

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    // Transform State (Pan & Zoom)
    private double _zoom = 1.0;
    private Point _panOffset = new(0, 0);
    private Point _lastMousePanPoint;
    private bool _isPanning = false;
    private Coordinate? _hoverCoordinate;

    private const double BaseCellSize = 36.0;
    private const double MinZoom = 0.4;
    private const double MaxZoom = 3.0;

    // Animation State
    private bool _isRenderingHooked = false;
    private bool _isMoveAnimating = false;
    private long _moveAnimStartTime;

    private bool _isWinAnimating = false;
    private long _winAnimStartTime;

    private bool _isCameraAnimating = false;
    private long _cameraAnimStartTime;
    private Point _startPan;
    private Point _targetPan;
    private double _startZoom;
    private double _targetZoom;

    // Brushes & Pens cached for ultra performance (Chess.com Dark Theme)
    private readonly Brush _backgroundBrush = new SolidColorBrush(Color.FromRgb(38, 36, 33)); // #262421
    private readonly Brush _boardSurfaceBrush = new SolidColorBrush(Color.FromRgb(48, 46, 43)); // #302E2B
    private readonly Pen _gridPen = new(new SolidColorBrush(Color.FromArgb(160, 68, 64, 59)), 1.0); // #44403B
    private readonly Pen _borderPen = new(new SolidColorBrush(Color.FromRgb(90, 85, 78)), 2.0); // #5A554E
    private readonly Brush _centerMarkerBrush = new SolidColorBrush(Color.FromArgb(160, 129, 182, 76)); // #81B64C

    // Player Brushes & Pens
    private readonly Pen _xPen = new(new SolidColorBrush(Color.FromRgb(239, 68, 68)), 3.4)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round
    };
    private readonly Pen _oPen = new(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 3.4);
    private readonly Brush _hoverBrush = new SolidColorBrush(Color.FromArgb(45, 129, 182, 76));
    private readonly Pen _lastMoveBorderPen = new(new SolidColorBrush(Color.FromArgb(220, 234, 179, 8)), 1.5);
    private readonly Pen _winningLinePen = new(new SolidColorBrush(Color.FromRgb(234, 179, 8)), 5.5)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round
    };
    private readonly Brush _winHaloBrush = new SolidColorBrush(Color.FromArgb(110, 234, 179, 8));

    public InfiniteCaroCanvas()
    {
        ClipToBounds = true;
        Focusable = true;

        _gridPen.Freeze();
        _borderPen.Freeze();
        _xPen.Freeze();
        _oPen.Freeze();
        _hoverBrush.Freeze();
        _lastMoveBorderPen.Freeze();
        _winningLinePen.Freeze();
        _winHaloBrush.Freeze();

        Loaded += (_, _) => ResetView(animated: false);
        SizeChanged += (_, _) => InvalidateVisual();
    }

    private static void OnBoardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfiniteCaroCanvas canvas)
        {
            if (e.OldValue is DynamicBoard oldBoard)
            {
                oldBoard.BoundsExpanded -= canvas.OnBoardBoundsExpanded;
            }
            if (e.NewValue is DynamicBoard newBoard)
            {
                newBoard.BoundsExpanded += canvas.OnBoardBoundsExpanded;
            }
            canvas.ResetView(animated: false);
            canvas.InvalidateVisual();
        }
    }

    private static void OnLastMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfiniteCaroCanvas canvas && e.NewValue is Coordinate)
        {
            canvas.TriggerMoveAnimation();
        }
    }

    private static void OnWinningLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfiniteCaroCanvas canvas && e.NewValue is IReadOnlyList<Coordinate> line && line.Count > 0)
        {
            canvas.TriggerWinningAnimation();
        }
    }

    private void TriggerMoveAnimation()
    {
        _isMoveAnimating = true;
        _moveAnimStartTime = Stopwatch.GetTimestamp();
        StartRenderLoop();
        InvalidateVisual();
    }

    private void TriggerWinningAnimation()
    {
        _isWinAnimating = true;
        _winAnimStartTime = Stopwatch.GetTimestamp();
        StartRenderLoop();
        InvalidateVisual();
    }

    private void StartRenderLoop()
    {
        if (!_isRenderingHooked)
        {
            _isRenderingHooked = true;
            CompositionTarget.Rendering += OnRenderingFrame;
        }
    }

    private void StopRenderLoop()
    {
        if (_isRenderingHooked)
        {
            _isRenderingHooked = false;
            CompositionTarget.Rendering -= OnRenderingFrame;
        }
    }

    private void OnRenderingFrame(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        bool needsRepaint = false;

        // Camera glide animation
        if (_isCameraAnimating)
        {
            double elapsed = (double)(now - _cameraAnimStartTime) / Stopwatch.Frequency;
            double progress = Math.Clamp(elapsed / 0.35, 0.0, 1.0);
            double ease = EaseOutCubic(progress);

            _panOffset = new Point(
                _startPan.X + (_targetPan.X - _startPan.X) * ease,
                _startPan.Y + (_targetPan.Y - _startPan.Y) * ease);
            _zoom = _startZoom + (_targetZoom - _startZoom) * ease;

            needsRepaint = true;
            if (progress >= 1.0) _isCameraAnimating = false;
        }

        // Move pop animation (280ms)
        if (_isMoveAnimating)
        {
            double elapsed = (double)(now - _moveAnimStartTime) / Stopwatch.Frequency;
            needsRepaint = true;
            if (elapsed >= 0.28) _isMoveAnimating = false;
        }

        // Winning line animation (1200ms)
        if (_isWinAnimating)
        {
            double elapsed = (double)(now - _winAnimStartTime) / Stopwatch.Frequency;
            needsRepaint = true;
            if (elapsed >= 1.2) _isWinAnimating = false;
        }

        if (needsRepaint)
        {
            InvalidateVisual();
        }
        else if (!_isCameraAnimating && !_isMoveAnimating && !_isWinAnimating)
        {
            StopRenderLoop();
        }
    }

    private static double EaseOutCubic(double t) => 1.0 - Math.Pow(1.0 - t, 3);

    private static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1.0;
        return 1.0 + c3 * Math.Pow(t - 1.0, 3) + c1 * Math.Pow(t - 1.0, 2);
    }

    private void OnBoardBoundsExpanded(BoardBounds _)
    {
        Dispatcher.InvokeAsync(InvalidateVisual);
    }

    public void ResetView(bool animated = true)
    {
        Point targetPan = new(ActualWidth / 2.0, ActualHeight / 2.0);
        double targetZoom = 1.0;

        if (!animated || ActualWidth <= 0 || !IsLoaded)
        {
            _zoom = targetZoom;
            _panOffset = targetPan;
            InvalidateVisual();
            return;
        }

        _startPan = _panOffset;
        _startZoom = _zoom;
        _targetPan = targetPan;
        _targetZoom = targetZoom;
        _cameraAnimStartTime = Stopwatch.GetTimestamp();
        _isCameraAnimating = true;
        StartRenderLoop();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Vẽ nền toàn canvas
        dc.DrawRectangle(_backgroundBrush, null, new Rect(0, 0, width, height));

        if (Board == null) return;

        BoardBounds bounds = Board.Bounds;
        double cellSize = BaseCellSize * _zoom;

        // Tọa độ góc trên bên trái của bàn cờ trên màn hình
        double boardLeft = _panOffset.X + bounds.MinCol * cellSize;
        double boardTop = _panOffset.Y + bounds.MinRow * cellSize;
        double boardWidth = bounds.ColCount * cellSize;
        double boardHeight = bounds.RowCount * cellSize;

        // 1. Vẽ mặt phẳng bàn cờ có đổ bóng/viền
        Rect boardRect = new(boardLeft, boardTop, boardWidth, boardHeight);
        dc.DrawRectangle(_boardSurfaceBrush, _borderPen, boardRect);

        // 2. Vẽ lưới ô cờ dạng ô vuông (các đường kẻ tạo thành mép ô vuông)
        for (int r = bounds.MinRow; r <= bounds.MaxRow + 1; r++)
        {
            double y = _panOffset.Y + r * cellSize;
            if (y >= -cellSize && y <= height + cellSize)
            {
                dc.DrawLine(_gridPen, new Point(boardLeft, y), new Point(boardLeft + boardWidth, y));
            }
        }

        for (int c = bounds.MinCol; c <= bounds.MaxCol + 1; c++)
        {
            double x = _panOffset.X + c * cellSize;
            if (x >= -cellSize && x <= width + cellSize)
            {
                dc.DrawLine(_gridPen, new Point(x, boardTop), new Point(x, boardTop + boardHeight));
            }
        }

        // 3. Đánh dấu nhẹ tâm tọa độ ô (0, 0)
        double centerCellX = _panOffset.X + 0.5 * cellSize;
        double centerCellY = _panOffset.Y + 0.5 * cellSize;
        dc.DrawEllipse(_centerMarkerBrush, null, new Point(centerCellX, centerCellY), 3.0 * _zoom, 3.0 * _zoom);

        // 4. Highlight ô hover nếu chuột đang rê vào (tô cả ô vuông)
        if (_hoverCoordinate.HasValue && Board.IsEmpty(_hoverCoordinate.Value) && IsInteractive)
        {
            Coordinate h = _hoverCoordinate.Value;
            double hx = _panOffset.X + h.Col * cellSize;
            double hy = _panOffset.Y + h.Row * cellSize;
            dc.DrawRoundedRectangle(_hoverBrush, null, new Rect(hx + 1.5, hy + 1.5, cellSize - 3, cellSize - 3), 3, 3);
        }

        // 5. Highlight nước cờ cuối cùng (LastMove) với hiệu ứng Breathing Glow
        long renderTime = Stopwatch.GetTimestamp();
        double moveAnimProgress = 1.0;
        if (_isMoveAnimating && LastMove.HasValue)
        {
            double elapsedSec = (double)(renderTime - _moveAnimStartTime) / Stopwatch.Frequency;
            moveAnimProgress = Math.Clamp(elapsedSec / 0.28, 0.0, 1.0);
        }

        if (LastMove.HasValue)
        {
            Coordinate lm = LastMove.Value;
            double lx = _panOffset.X + lm.Col * cellSize;
            double ly = _panOffset.Y + lm.Row * cellSize;

            // Nhịp thở mượt mà (smooth breathing pulse)
            double pulseTime = (double)renderTime / Stopwatch.Frequency;
            double pulseVal = (Math.Sin(pulseTime * 3.5) + 1.0) / 2.0;
            byte glowAlpha = (byte)(40 + pulseVal * 35);
            Brush pulseBrush = new SolidColorBrush(Color.FromArgb(glowAlpha, 245, 158, 11));

            dc.DrawRoundedRectangle(pulseBrush, _lastMoveBorderPen, new Rect(lx + 1.5, ly + 1.5, cellSize - 3, cellSize - 3), 4, 4);
        }

        // 6. Vẽ các quân cờ đánh VÀO BÊN TRONG Ô VUÔNG (kèm Animation Pop-in & Ripple)
        double baseSpan = (cellSize / 2.0) * 0.65;

        foreach (var (coord, state) in Board.GetOccupiedCells())
        {
            double cx = _panOffset.X + (coord.Col + 0.5) * cellSize;
            double cy = _panOffset.Y + (coord.Row + 0.5) * cellSize;

            // Viewport culling
            if (cx < -cellSize || cx > width + cellSize || cy < -cellSize || cy > height + cellSize)
                continue;

            // Tính scale đàn hồi (EaseOutBack) nếu là nước cờ mới đánh
            double scale = 1.0;
            if (Equals(coord, LastMove) && moveAnimProgress < 1.0)
            {
                scale = EaseOutBack(moveAnimProgress);
            }

            double span = baseSpan * Math.Max(0.01, scale);

            if (state == CellState.X)
            {
                dc.DrawLine(_xPen, new Point(cx - span, cy - span), new Point(cx + span, cy + span));
                dc.DrawLine(_xPen, new Point(cx + span, cy - span), new Point(cx - span, cy + span));
            }
            else if (state == CellState.O)
            {
                dc.DrawEllipse(null, _oPen, new Point(cx, cy), span, span);
            }

            // Sóng năng lượng Ripple lan tỏa ra từ quân cờ vừa hạ xuống
            if (Equals(coord, LastMove) && moveAnimProgress < 1.0)
            {
                double rippleRadius = (cellSize * 0.25) + moveAnimProgress * (cellSize * 0.65);
                byte alpha = (byte)((1.0 - moveAnimProgress) * 160);
                Color rippleColor = state == CellState.X
                    ? Color.FromArgb(alpha, 225, 29, 72)
                    : Color.FromArgb(alpha, 37, 99, 235);
                Brush rippleBrush = new SolidColorBrush(rippleColor);
                Pen ripplePen = new(rippleBrush, 2.0);
                dc.DrawEllipse(null, ripplePen, new Point(cx, cy), rippleRadius, rippleRadius);
            }
        }

        // 7. Vẽ đường kẻ chiến thắng mượt mà (Animated Victory Line Strike)
        if (WinningLine != null && WinningLine.Count >= 2)
        {
            double winProgress = 1.0;
            if (_isWinAnimating)
            {
                double elapsedSec = (double)(renderTime - _winAnimStartTime) / Stopwatch.Frequency;
                winProgress = Math.Clamp(elapsedSec / 0.55, 0.0, 1.0);
            }
            double winEase = EaseOutCubic(winProgress);

            Coordinate first = WinningLine[0];
            Coordinate last = WinningLine[^1];
            Point p1 = new(_panOffset.X + (first.Col + 0.5) * cellSize, _panOffset.Y + (first.Row + 0.5) * cellSize);
            Point p2 = new(_panOffset.X + (last.Col + 0.5) * cellSize, _panOffset.Y + (last.Row + 0.5) * cellSize);

            Point currentHead = new(p1.X + (p2.X - p1.X) * winEase, p1.Y + (p2.Y - p1.Y) * winEase);
            dc.DrawLine(_winningLinePen, p1, currentHead);

            // Vòng hào quang vàng tỏa sáng trên 5 ô chiến thắng
            for (int i = 0; i < WinningLine.Count; i++)
            {
                double fraction = (double)i / (WinningLine.Count - 1);
                if (winEase >= fraction)
                {
                    Coordinate wc = WinningLine[i];
                    double wcx = _panOffset.X + (wc.Col + 0.5) * cellSize;
                    double wcy = _panOffset.Y + (wc.Row + 0.5) * cellSize;
                    dc.DrawEllipse(_winHaloBrush, null, new Point(wcx, wcy), cellSize * 0.42, cellSize * 0.42);
                }
            }
        }
    }

    private Coordinate PointToCoordinate(Point p)
    {
        double cellSize = BaseCellSize * _zoom;
        int col = (int)Math.Floor((p.X - _panOffset.X) / cellSize);
        int row = (int)Math.Floor((p.Y - _panOffset.Y) / cellSize);
        return new Coordinate(row, col);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Right)
        {
            _isPanning = true;
            _lastMousePanPoint = e.GetPosition(this);
            CaptureMouse();
            Cursor = Cursors.SizeAll;
        }
        else if (e.ChangedButton == MouseButton.Middle)
        {
            ResetView();
        }
        else if (e.ChangedButton == MouseButton.Left && IsInteractive && Board != null)
        {
            Point mousePos = e.GetPosition(this);
            Coordinate clickedCoord = PointToCoordinate(mousePos);

            if (Board.IsEmpty(clickedCoord))
            {
                if (CellClickedCommand != null && CellClickedCommand.CanExecute(clickedCoord))
                {
                    CellClickedCommand.Execute(clickedCoord);
                    InvalidateVisual();
                }
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        Point currentPos = e.GetPosition(this);

        if (_isPanning)
        {
            Vector delta = currentPos - _lastMousePanPoint;
            _panOffset = new Point(_panOffset.X + delta.X, _panOffset.Y + delta.Y);
            _lastMousePanPoint = currentPos;
            InvalidateVisual();
        }
        else
        {
            Coordinate coord = PointToCoordinate(currentPos);
            if (!Equals(coord, _hoverCoordinate))
            {
                _hoverCoordinate = coord;
                InvalidateVisual();
            }
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Right && _isPanning)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        Point mousePos = e.GetPosition(this);
        double zoomFactor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        double newZoom = Math.Clamp(_zoom * zoomFactor, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - _zoom) > 0.001)
        {
            // Zoom hướng về vị trí con trỏ chuột
            double scale = newZoom / _zoom;
            _panOffset = new Point(
                mousePos.X - (mousePos.X - _panOffset.X) * scale,
                mousePos.Y - (mousePos.Y - _panOffset.Y) * scale);

            _zoom = newZoom;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverCoordinate = null;
        InvalidateVisual();
    }
}
