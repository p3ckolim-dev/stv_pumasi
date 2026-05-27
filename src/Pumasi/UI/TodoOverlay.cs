using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pumasi.Core.Tasks;
using Pumasi.Multiplayer;
using Pumasi.Services;
using StardewModdingAPI;
using StardewValley;

namespace Pumasi.UI;

internal sealed class TodoOverlay
{
    public bool Visible { get; set; } = true;

    public void Draw(SpriteBatch spriteBatch, TodoSnapshot snapshot, HelperRuntimeState localState, HelperStateMessage? syncedState)
    {
        if (!Visible || !Context.IsWorldReady)
            return;

        var stateName = syncedState?.Name ?? localState.Name;
        var status = syncedState?.Status ?? localState.Status;
        var position = new Vector2(24, 92);
        var lineHeight = Game1.smallFont.LineSpacing + 4;

        DrawShadowedText(spriteBatch, $"{stateName}: {status}", position, Color.LightGreen);
        position.Y += lineHeight;

        var visibleItems = snapshot.Items
            .Where(item => item.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress)
            .Take(8)
            .ToArray();

        if (visibleItems.Length == 0)
        {
            DrawShadowedText(spriteBatch, "Todo: idle", position, Color.White);
            return;
        }

        for (var i = 0; i < visibleItems.Length; i++)
        {
            var item = visibleItems[i];
            var text = $"#{i + 1} [{item.Status}] {item.Type} {item.Location}({item.X},{item.Y})";
            DrawShadowedText(spriteBatch, text, position, Color.White);
            position.Y += lineHeight;
        }
    }

    private static void DrawShadowedText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
    {
        spriteBatch.DrawString(Game1.smallFont, text, position + new Vector2(2, 2), Color.Black * 0.75f);
        spriteBatch.DrawString(Game1.smallFont, text, position, color);
    }
}
