using CaroGame.Core.Enums;

namespace CaroGame.Core.Models;

public class DynamicBoard
{
    private readonly Dictionary<Coordinate, CellState> _cells = new();
    private BoardBounds _bounds;
    private readonly int _expandMargin;
    private readonly int _expandStep;

    public event Action<BoardBounds>? BoundsExpanded;

    public DynamicBoard(int initialHalfSize = 7, int expandMargin = 2, int expandStep = 4)
    {
        _bounds = BoardBounds.CreateSymmetric(initialHalfSize);
        _expandMargin = expandMargin;
        _expandStep = expandStep;
    }

    public BoardBounds Bounds => _bounds;

    public int PlacedCount => _cells.Count;

    public CellState GetCell(Coordinate coord)
    {
        return _cells.TryGetValue(coord, out CellState state) ? state : CellState.Empty;
    }

    public bool IsEmpty(Coordinate coord)
    {
        return GetCell(coord) == CellState.Empty;
    }

    public bool SetCell(Coordinate coord, CellState state)
    {
        if (state == CellState.Empty)
        {
            _cells.Remove(coord);
            return true;
        }

        if (_cells.ContainsKey(coord))
        {
            return false;
        }

        _cells[coord] = state;
        CheckAndExpandBounds(coord);
        return true;
    }

    public void RemoveCell(Coordinate coord)
    {
        _cells.Remove(coord);
    }

    public void Clear()
    {
        _cells.Clear();
    }

    public void Reset(int initialHalfSize = 7)
    {
        _cells.Clear();
        _bounds = BoardBounds.CreateSymmetric(initialHalfSize);
    }

    public IReadOnlyDictionary<Coordinate, CellState> GetOccupiedCells() => _cells;

    public void CheckAndExpandBounds(Coordinate lastMove)
    {
        int minRow = _bounds.MinRow;
        int maxRow = _bounds.MaxRow;
        int minCol = _bounds.MinCol;
        int maxCol = _bounds.MaxCol;
        bool expanded = false;

        if (lastMove.Row - minRow <= _expandMargin)
        {
            minRow -= _expandStep;
            expanded = true;
        }

        if (maxRow - lastMove.Row <= _expandMargin)
        {
            maxRow += _expandStep;
            expanded = true;
        }

        if (lastMove.Col - minCol <= _expandMargin)
        {
            minCol -= _expandStep;
            expanded = true;
        }

        if (maxCol - lastMove.Col <= _expandMargin)
        {
            maxCol += _expandStep;
            expanded = true;
        }

        if (expanded)
        {
            _bounds = new BoardBounds(minRow, maxRow, minCol, maxCol);
            BoundsExpanded?.Invoke(_bounds);
        }
    }

    public List<Coordinate> GetCandidateMoves(int distance = 2)
    {
        if (_cells.Count == 0)
        {
            return new List<Coordinate> { Coordinate.Center };
        }

        HashSet<Coordinate> candidates = new();

        foreach (Coordinate occupied in _cells.Keys)
        {
            for (int dr = -distance; dr <= distance; dr++)
            {
                for (int dc = -distance; dc <= distance; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    Coordinate neighbor = occupied.Offset(dr, dc);
                    if (!_cells.ContainsKey(neighbor))
                    {
                        candidates.Add(neighbor);
                    }
                }
            }
        }

        return candidates.Count > 0 ? candidates.ToList() : new List<Coordinate> { Coordinate.Center };
    }

    public DynamicBoard Clone()
    {
        DynamicBoard clone = new(0, _expandMargin, _expandStep)
        {
            _bounds = this._bounds
        };
        foreach (KeyValuePair<Coordinate, CellState> kvp in _cells)
        {
            clone._cells[kvp.Key] = kvp.Value;
        }
        return clone;
    }
}
