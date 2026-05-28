using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Pumasi.Multiplayer;
using Pumasi.Services;
using StardewModdingAPI;
using StardewValley;

namespace Pumasi.UI;

internal sealed class TodoOverlay
{
    private static readonly Color BoardShadow = new(35, 18, 8, 150);
    private static readonly Color BoardEdge = new(82, 38, 12, 245);
    private static readonly Color BoardWood = new(156, 86, 25, 245);
    private static readonly Color BoardHighlight = new(222, 142, 38, 230);
    private static readonly Color BoardInset = new(91, 45, 18, 230);
    private static readonly Color BoardParchment = new(235, 205, 128, 220);
    private static readonly Color BoardParchmentShade = new(169, 111, 43, 90);
    private static readonly Color TitleText = new(38, 105, 45);
    private static readonly Color BodyText = new(86, 53, 27);
    private static readonly Color ButtonFill = new(126, 70, 24, 235);
    private static readonly Color ButtonText = new(255, 236, 177);

    private IReadOnlyList<TodoReorderControl> reorderControls = Array.Empty<TodoReorderControl>();

    public bool Visible { get; set; } = true;

    public bool TryResolveReorderClick(int x, int y, out TodoReorderMove move)
    {
        if (Visible)
            return TodoOverlayLayout.TryResolveReorderClick(reorderControls, x, y, out move);

        move = new TodoReorderMove(0, 0);
        return false;
    }

    public void Draw(SpriteBatch spriteBatch, TodoSnapshot snapshot, HelperRuntimeState localState, HelperStateMessage? syncedState)
    {
        reorderControls = Array.Empty<TodoReorderControl>();

        if (!Visible || !Context.IsWorldReady)
            return;

        var stateName = syncedState?.Name ?? localState.Name;
        var status = syncedState?.Status ?? localState.Status;
        var lineHeight = Game1.smallFont.LineSpacing + 4;
        var maxTodoRows = TodoOverlayLayout.GetVisibleTodoCapacity(lineHeight, Game1.uiViewport.Height);

        var visibleItems = snapshot.Items
            .Where(item => item.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress)
            .Take(maxTodoRows)
            .ToArray();
        var panel = TodoOverlayLayout.Create(visibleItems.Length, lineHeight, Game1.uiViewport.Width, Game1.uiViewport.Height);
        var position = new Vector2(panel.TextX, panel.TextY);
        var canReorder = Context.IsMainPlayer && visibleItems.Length > 1;
        if (canReorder)
            reorderControls = TodoOverlayLayout.CreateReorderControls(panel, visibleItems.Length, lineHeight);
        var textWidth = GetTodoTextWidth(panel, reorderControls);

        DrawBoard(spriteBatch, panel);
        DrawShadowedText(spriteBatch, $"{stateName}: {status}", position, TitleText);
        position.Y += lineHeight;

        if (visibleItems.Length == 0)
        {
            DrawShadowedText(spriteBatch, "Todo: idle", position, BodyText);
            return;
        }

        for (var i = 0; i < visibleItems.Length; i++)
        {
            var item = visibleItems[i];
            var text = $"#{i + 1} [{item.Status}] {item.Type} {item.Location}({item.X},{item.Y})";
            DrawShadowedText(spriteBatch, TrimToWidth(text, textWidth), position, BodyText);
            DrawReorderControls(spriteBatch, reorderControls.Where(control => control.FromPosition == i + 1));
            position.Y += lineHeight;
        }
    }

    private static void DrawBoard(SpriteBatch spriteBatch, TodoOverlayPanel panel)
    {
        var bounds = new Rectangle(panel.X, panel.Y, panel.Width, panel.Height);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), BoardShadow);
        DrawRectangle(spriteBatch, bounds, BoardEdge);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8), BoardWood);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, bounds.Height - 16), BoardInset);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 14, bounds.Y + 14, bounds.Width - 28, bounds.Height - 28), BoardParchment);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 14, bounds.Y + 14, bounds.Width - 28, 3), Color.White * 0.22f);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 14, bounds.Bottom - 17, bounds.Width - 28, 3), BoardParchmentShade);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 10, bounds.Y + 10, bounds.Width - 20, 3), BoardHighlight);
    }

    private static void DrawRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        spriteBatch.Draw(Game1.staminaRect, bounds, color);
    }

    private static int GetTodoTextWidth(TodoOverlayPanel panel, IReadOnlyList<TodoReorderControl> controls)
    {
        if (controls.Count == 0)
            return panel.InnerWidth;

        return Math.Max(120, controls.Min(control => control.Bounds.X) - panel.TextX - 8);
    }

    private static void DrawReorderControls(SpriteBatch spriteBatch, IEnumerable<TodoReorderControl> controls)
    {
        foreach (var control in controls)
            DrawReorderControl(spriteBatch, control);
    }

    private static void DrawReorderControl(SpriteBatch spriteBatch, TodoReorderControl control)
    {
        var bounds = new Rectangle(control.Bounds.X, control.Bounds.Y, control.Bounds.Width, control.Bounds.Height);
        DrawRectangle(spriteBatch, bounds, BoardEdge);
        DrawRectangle(spriteBatch, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), ButtonFill);

        var label = control.Direction == TodoReorderDirection.Up ? "^" : "v";
        var size = Game1.smallFont.MeasureString(label);
        var position = new Vector2(
            bounds.X + (bounds.Width - size.X) / 2f,
            bounds.Y + (bounds.Height - size.Y) / 2f - 2f);
        spriteBatch.DrawString(Game1.smallFont, label, position + new Vector2(1, 1), Color.Black * 0.45f);
        spriteBatch.DrawString(Game1.smallFont, label, position, ButtonText);
    }

    private static void DrawShadowedText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
    {
        spriteBatch.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Color.White * 0.55f);
        spriteBatch.DrawString(Game1.smallFont, text, position, color);
    }

    private static string TrimToWidth(string text, int maxWidth)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth)
            return text;

        const string suffix = "...";
        var trimmed = text;
        while (trimmed.Length > suffix.Length && Game1.smallFont.MeasureString(trimmed + suffix).X > maxWidth)
            trimmed = trimmed[..^1];

        return trimmed.Length <= suffix.Length ? suffix : trimmed + suffix;
    }
}
