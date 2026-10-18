using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public partial class BoardControl : Control
{
    private readonly Dictionary<char, string> _glyphs = new()
    {
        ['K'] = "♚",
        ['Q'] = "♛",
        ['R'] = "♜",
        ['B'] = "♝",
        ['N'] = "♞",
        ['P'] = "♟",
        ['k'] = "♚",
        ['q'] = "♛",
        ['r'] = "♜",
        ['b'] = "♝",
        ['n'] = "♞",
        ['p'] = "♟"
    };
    private Position? position;
    private string? displayedFen;
    private char[]? displayedSquares;
    public int? LastMoveDestination { get; private set; }
    public Position? Position
    {
        get => position;
        set
        {
            string? fen = value?.ToFen();
            if (value is not null && position is not null && displayedFen?.Split(' ')[1] != (value.WhiteToMove ? "w" : "b"))
                StartTurnTransition(value.WhiteToMove, 250);
            if (displayedFen != fen || !ReferenceEquals(position, value))
            {
                if (displayedFen != fen)
                {
                    LastMoveDestination = null;
                    if (value is not null && displayedSquares is not null && displayedFen?.Split(' ')[1] != (value.WhiteToMove ? "w" : "b"))
                    {
                        var arrived = Enumerable.Range(0, 64).Where(s => value.Squares[s] != '\0' && char.IsUpper(value.Squares[s]) != value.WhiteToMove && value.Squares[s] != displayedSquares[s]).ToArray();
                        if (arrived.Length == 1) LastMoveDestination = arrived[0];
                        else if (arrived.Length == 2 && arrived.Any(s => char.ToLowerInvariant(value.Squares[s]) == 'k'))
                            LastMoveDestination = arrived.First(s => char.ToLowerInvariant(value.Squares[s]) == 'k');
                    }
                }
                SelectedSquare = null;
                CandidateMoves = [];
                HintMove = null;
                HintColor = null;
                Assessment = null;
                ErrorSquares.Clear();
                ErrorArrows.Clear();
                _leftStart = null;
                _dragging = false;
                PreviewArrow = null;
            }

            position = value;
            displayedFen = fen;
            displayedSquares = value?.Squares.ToArray();
        }
    }

    public string? Outcome { get; set; }
    public string? WhitePlayerName { get; set; }
    public string? BlackPlayerName { get; set; }
    public Move? QueuedMove { get; set; }
    public bool? PremoveSide { get; set; }
    public bool Flip { get; set; }
    public int? SelectedSquare { get; set; }
    public IReadOnlyList<ChessMove> CandidateMoves { get; set; } = [];
    public Func<Position, IReadOnlyList<ChessMove>>? MoveProvider { get; set; }
    public bool ShowDestinationHints { get; set; } = true;
    public ChessMove? HintMove { get; set; }
    public Color? HintColor { get; set; }
    public RectangleF PlayingBounds => new(BoardLeft, 22, BoardSize, BoardSize);
    public HashSet<int> MarkedSquares { get; } = [];
    public HashSet<int> ErrorSquares { get; } = [];
    public (int Square, string Category)? Assessment { get; set; }
    public List<BoardArrow> Arrows { get; } = [];
    public HashSet<BoardArrow> ErrorArrows { get; } = [];
    public BoardArrow? PreviewArrow { get; private set; }

    public event Action<int>? SquareClicked;
    public event Action<int, int>? SquareDragged;
    public event Action? BlankClicked;
    private int? _leftStart;
    private int? _rightStart;
    private Point _pointerStart;
    private bool _dragging;
    private Point _pointer;
    public bool InputEnabled { get; set; } = true;

    private List<(char Piece, int From, int To)> animated = [];
    private double animationFraction;
    private int animationId;
    public BoardControl()
    {
        SetStyle(ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        MinimumSize = new Size(360, 360);
        Cursor = Cursors.Hand;
        Preferences.Changed += AppearanceChanged;
    }

    private void AppearanceChanged()
    {
        if (!IsDisposed)
            Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !animationDisposed)
        {
            animationLifetime.Cancel();
            turnTimer.Dispose();
            Preferences.Changed -= AppearanceChanged;
        }
        base.Dispose(disposing);
        if (disposing && !animationDisposed) { animationDisposed = true; animationLifetime.Dispose(); }
    }

    private float BoardSize => Math.Max(8, Math.Min(Width - 16, Height - 44));
    private float BoardLeft => (Width - BoardSize) / 2;
}
