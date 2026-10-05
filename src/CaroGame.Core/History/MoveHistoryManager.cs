using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.History;

public class MoveHistoryManager
{
    private readonly Stack<MoveRecord> _undoStack = new();
    private readonly Stack<MoveRecord> _redoStack = new();

    public event Action? HistoryChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int MoveCount => _undoStack.Count;

    public IReadOnlyList<MoveRecord> AllMoves => _undoStack.Reverse().ToList();

    public void RecordMove(Coordinate coord, CellState player)
    {
        int turnNumber = _undoStack.Count + 1;
        MoveRecord record = new(turnNumber, player, coord, DateTime.UtcNow);
        _undoStack.Push(record);
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    public MoveRecord? Undo(DynamicBoard board)
    {
        if (_undoStack.Count == 0) return null;

        MoveRecord record = _undoStack.Pop();
        board.RemoveCell(record.Coord);
        _redoStack.Push(record);
        HistoryChanged?.Invoke();
        return record;
    }

    public MoveRecord? Redo(DynamicBoard board)
    {
        if (_redoStack.Count == 0) return null;

        MoveRecord record = _redoStack.Pop();
        board.SetCell(record.Coord, record.Player);
        _undoStack.Push(record);
        HistoryChanged?.Invoke();
        return record;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}
