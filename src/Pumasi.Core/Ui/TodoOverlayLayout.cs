namespace Pumasi.Core.Ui;

public enum TodoReorderDirection
{
    Up,
    Down
}

public sealed record TodoOverlayBounds(int X, int Y, int Width, int Height)
{
    public bool Contains(int x, int y)
    {
        return x >= X && x < X + Width && y >= Y && y < Y + Height;
    }
}

public sealed record TodoReorderMove(int FromPosition, int ToPosition);

public sealed record TodoReorderControl(
    TodoReorderDirection Direction,
    int FromPosition,
    int ToPosition,
    TodoOverlayBounds Bounds);

public sealed record TodoOverlayPanel(int X, int Y, int Width, int Height, int Padding, int LineCount)
{
    public int TextX => X + Padding;
    public int TextY => Y + Padding;
    public int Bottom => Y + Height;
    public int InnerWidth => Width - Padding * 2;
}

public static class TodoOverlayLayout
{
    private const int Margin = 24;
    private const int DefaultX = 24;
    private const int DefaultY = 320;
    private const int PanelWidth = 520;
    private const int MinPanelWidth = 240;
    private const int Padding = 18;
    private const int IconSize = 64;
    private const int PopupGap = 12;
    private const int MaxTodoRows = 8;
    private const int ReorderButtonSize = 22;
    private const int ReorderButtonGap = 4;

    public static TodoOverlayBounds CreateIcon(int viewportWidth, int viewportHeight)
    {
        var maxX = Math.Max(Margin, viewportWidth - IconSize - Margin);
        var maxY = Math.Max(Margin, viewportHeight - IconSize - Margin);
        var x = Math.Clamp(DefaultX, Margin, maxX);
        var y = Math.Clamp(DefaultY, Margin, maxY);

        return new TodoOverlayBounds(x, y, IconSize, IconSize);
    }

    public static bool TryResolveIconClick(TodoOverlayBounds icon, int x, int y)
    {
        return icon.Contains(x, y);
    }

    public static TodoOverlayPanel Create(int activeTodoCount, int lineHeight, int viewportWidth, int viewportHeight)
    {
        if (lineHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");

        var todoLines = Math.Max(1, Math.Min(activeTodoCount, GetVisibleTodoCapacity(lineHeight, viewportHeight)));
        var lineCount = 1 + todoLines;
        var availableWidth = Math.Max(MinPanelWidth, viewportWidth - Margin * 2);
        var width = Math.Min(PanelWidth, availableWidth);
        var height = Padding * 2 + lineHeight * lineCount;
        var maxX = Math.Max(Margin, viewportWidth - width - Margin);
        var maxY = Math.Max(Margin, viewportHeight - height - Margin);
        var x = Math.Clamp(DefaultX, Margin, maxX);
        var y = Math.Clamp(DefaultY, Margin, maxY);

        return new TodoOverlayPanel(x, y, width, height, Padding, lineCount);
    }

    public static TodoOverlayPanel CreatePopup(
        int activeTodoCount,
        int lineHeight,
        int viewportWidth,
        int viewportHeight,
        TodoOverlayBounds icon)
    {
        var panel = Create(activeTodoCount, lineHeight, viewportWidth, viewportHeight);
        var preferredX = icon.X + icon.Width + PopupGap;
        var maxX = Math.Max(Margin, viewportWidth - panel.Width - Margin);
        var x = Math.Clamp(preferredX, Margin, maxX);
        var y = Math.Clamp(icon.Y, Margin, Math.Max(Margin, viewportHeight - panel.Height - Margin));

        return panel with { X = x, Y = y };
    }

    public static int GetVisibleTodoCapacity(int lineHeight, int viewportHeight)
    {
        if (lineHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");

        var availableHeight = Math.Max(lineHeight * 2 + Padding * 2, viewportHeight - Margin * 2);
        var lineCapacity = Math.Max(2, (availableHeight - Padding * 2) / lineHeight);

        return Math.Clamp(lineCapacity - 1, 1, MaxTodoRows);
    }

    public static IReadOnlyList<TodoReorderControl> CreateReorderControls(TodoOverlayPanel panel, int activeTodoCount, int lineHeight)
    {
        if (activeTodoCount <= 1)
            return Array.Empty<TodoReorderControl>();

        if (lineHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");

        var controls = new List<TodoReorderControl>();
        var downX = panel.X + panel.Width - panel.Padding - ReorderButtonSize;
        var upX = downX - ReorderButtonGap - ReorderButtonSize;
        for (var position = 1; position <= activeTodoCount; position++)
        {
            var y = panel.TextY + lineHeight * position + Math.Max(0, (lineHeight - ReorderButtonSize) / 2);

            if (position > 1)
            {
                controls.Add(new TodoReorderControl(
                    TodoReorderDirection.Up,
                    position,
                    position - 1,
                    new TodoOverlayBounds(upX, y, ReorderButtonSize, ReorderButtonSize)));
            }

            if (position < activeTodoCount)
            {
                controls.Add(new TodoReorderControl(
                    TodoReorderDirection.Down,
                    position,
                    position + 1,
                    new TodoOverlayBounds(downX, y, ReorderButtonSize, ReorderButtonSize)));
            }
        }

        return controls;
    }

    public static bool TryResolveReorderClick(IEnumerable<TodoReorderControl> controls, int x, int y, out TodoReorderMove move)
    {
        foreach (var control in controls)
        {
            if (!control.Bounds.Contains(x, y))
                continue;

            move = new TodoReorderMove(control.FromPosition, control.ToPosition);
            return true;
        }

        move = new TodoReorderMove(0, 0);
        return false;
    }
}
