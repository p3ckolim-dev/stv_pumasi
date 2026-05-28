using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pumasi.Core.Configuration;
using Pumasi.Core.Ui;
using StardewValley;
using StardewValley.Menus;

namespace Pumasi.UI;

internal sealed class PumasiSettingsPage : IClickableMenu
{
    private const int ContentInset = 88;
    private const int RowHeight = 56;
    private const int CheckboxSize = 32;
    private const int CheckboxTextGap = 20;
    private const int FooterHeight = 44;

    private static readonly Color TextColor = new(86, 22, 12);
    private static readonly Color MutedTextColor = new(126, 81, 47);
    private static readonly Color CheckboxBorder = new(92, 47, 19);
    private static readonly Color CheckboxFill = new(242, 180, 91);
    private static readonly Color CheckboxEnabledFill = new(76, 181, 63);
    private static readonly Color CheckboxDisabledFill = new(132, 105, 75);

    private readonly ModConfig config;
    private readonly Func<bool> canEditHostSettings;
    private readonly Action save;
    private readonly Action changed;
    private readonly IReadOnlyList<PumasiSettingsRow> rows;
    private readonly List<RowHitArea> rowHitAreas = new();

    public PumasiSettingsPage(
        int x,
        int y,
        int width,
        int height,
        ModConfig config,
        Func<bool> canEditHostSettings,
        Action save,
        Action changed)
        : base(x, y, width, height)
    {
        this.config = config;
        this.canEditHostSettings = canEditHostSettings;
        this.save = save;
        this.changed = changed;
        rows = PumasiSettingsCatalog.CreateRows();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var hitArea in rowHitAreas)
        {
            if (!hitArea.Bounds.Contains(x, y))
                continue;

            if (!CanEdit(hitArea.Row.Key))
            {
                Game1.playSound("cancel");
                return;
            }

            Toggle(hitArea.Row.Key);
            save();
            changed();
            Game1.playSound("drumkit6");
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        rowHitAreas.Clear();
        DrawTitle(b);

        var contentX = xPositionOnScreen + ContentInset;
        var contentY = yPositionOnScreen + ContentInset + 34;
        var contentWidth = width - ContentInset * 2;
        var visibleRows = Math.Max(1, (height - ContentInset * 2 - FooterHeight) / RowHeight);

        for (var i = 0; i < rows.Count && i < visibleRows; i++)
        {
            var row = rows[i];
            var rowY = contentY + i * RowHeight;
            var checkboxBounds = new Rectangle(contentX, rowY + 10, CheckboxSize, CheckboxSize);
            var rowBounds = new Rectangle(contentX, rowY, contentWidth, RowHeight);
            var enabled = GetValue(row.Key);
            var editable = CanEdit(row.Key);

            DrawCheckbox(b, checkboxBounds, enabled, editable);
            DrawRowLabel(b, row, new Vector2(contentX + CheckboxSize + CheckboxTextGap, rowY + 5), editable);
            rowHitAreas.Add(new RowHitArea(row, rowBounds));
        }

        DrawFooter(b);
    }

    private void DrawTitle(SpriteBatch b)
    {
        var title = "Pumasi Settings";
        var subtitle = canEditHostSettings()
            ? "품앗이 빠른 설정"
            : "게스트는 로컬 UI 설정만 변경할 수 있어요";
        var titlePosition = new Vector2(xPositionOnScreen + ContentInset, yPositionOnScreen + 62);
        var subtitlePosition = new Vector2(titlePosition.X, titlePosition.Y + Game1.dialogueFont.LineSpacing - 2);

        b.DrawString(Game1.dialogueFont, title, titlePosition + new Vector2(2, 2), Color.White * 0.55f);
        b.DrawString(Game1.dialogueFont, title, titlePosition, TextColor);
        b.DrawString(Game1.smallFont, subtitle, subtitlePosition + new Vector2(1, 1), Color.White * 0.45f);
        b.DrawString(Game1.smallFont, subtitle, subtitlePosition, MutedTextColor);
    }

    private void DrawFooter(SpriteBatch b)
    {
        var text = "Text settings and Gemini API key: use Generic Mod Config Menu or SMAPI console pms_key.";
        var position = new Vector2(xPositionOnScreen + ContentInset, yPositionOnScreen + height - ContentInset + 22);
        b.DrawString(Game1.smallFont, TrimToWidth(text, width - ContentInset * 2), position + new Vector2(1, 1), Color.White * 0.45f);
        b.DrawString(Game1.smallFont, TrimToWidth(text, width - ContentInset * 2), position, MutedTextColor);
    }

    private static void DrawCheckbox(SpriteBatch b, Rectangle bounds, bool enabled, bool editable)
    {
        var fill = editable
            ? enabled ? CheckboxEnabledFill : CheckboxFill
            : CheckboxDisabledFill;

        DrawRectangle(b, bounds, CheckboxBorder);
        DrawRectangle(b, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8), fill);

        if (!enabled)
            return;

        var mark = "X";
        var markSize = Game1.smallFont.MeasureString(mark);
        var markPosition = new Vector2(
            bounds.X + (bounds.Width - markSize.X) / 2f,
            bounds.Y + (bounds.Height - markSize.Y) / 2f - 2f);
        b.DrawString(Game1.smallFont, mark, markPosition + new Vector2(1, 1), Color.Black * 0.3f);
        b.DrawString(Game1.smallFont, mark, markPosition, Color.White);
    }

    private static void DrawRowLabel(SpriteBatch b, PumasiSettingsRow row, Vector2 position, bool editable)
    {
        var color = editable ? TextColor : MutedTextColor;
        var label = $"{row.KoreanLabel}  /  {row.EnglishLabel}";
        b.DrawString(Game1.smallFont, label, position + new Vector2(1, 1), Color.White * 0.4f);
        b.DrawString(Game1.smallFont, label, position, color);
    }

    private bool GetValue(PumasiSettingsKey key)
    {
        return key switch
        {
            PumasiSettingsKey.ShowTodoOverlay => config.Ui.ShowTodoOverlay,
            PumasiSettingsKey.ShowHelperStatusNotifications => config.Ui.ShowHelperStatusNotifications,
            PumasiSettingsKey.WorkCrops => config.Assistant.WorkCategories.Crops,
            PumasiSettingsKey.WorkMachines => config.Assistant.WorkCategories.Machines,
            PumasiSettingsKey.WorkAnimals => config.Assistant.WorkCategories.Animals,
            PumasiSettingsKey.WorkChests => config.Assistant.WorkCategories.Chests,
            PumasiSettingsKey.WorkPlanting => config.Assistant.WorkCategories.Planting,
            PumasiSettingsKey.WikiAnswersEnabled => config.WikiAnswers.WikiAnswersEnabled,
            _ => false
        };
    }

    private void Toggle(PumasiSettingsKey key)
    {
        switch (key)
        {
            case PumasiSettingsKey.ShowTodoOverlay:
                config.Ui.ShowTodoOverlay = !config.Ui.ShowTodoOverlay;
                break;

            case PumasiSettingsKey.ShowHelperStatusNotifications:
                config.Ui.ShowHelperStatusNotifications = !config.Ui.ShowHelperStatusNotifications;
                break;

            case PumasiSettingsKey.WorkCrops:
                config.Assistant.WorkCategories.Crops = !config.Assistant.WorkCategories.Crops;
                break;

            case PumasiSettingsKey.WorkMachines:
                config.Assistant.WorkCategories.Machines = !config.Assistant.WorkCategories.Machines;
                break;

            case PumasiSettingsKey.WorkAnimals:
                config.Assistant.WorkCategories.Animals = !config.Assistant.WorkCategories.Animals;
                break;

            case PumasiSettingsKey.WorkChests:
                config.Assistant.WorkCategories.Chests = !config.Assistant.WorkCategories.Chests;
                break;

            case PumasiSettingsKey.WorkPlanting:
                config.Assistant.WorkCategories.Planting = !config.Assistant.WorkCategories.Planting;
                break;

            case PumasiSettingsKey.WikiAnswersEnabled:
                config.WikiAnswers.WikiAnswersEnabled = !config.WikiAnswers.WikiAnswersEnabled;
                break;
        }
    }

    private bool CanEdit(PumasiSettingsKey key)
    {
        if (key is PumasiSettingsKey.ShowTodoOverlay or PumasiSettingsKey.ShowHelperStatusNotifications)
            return true;

        return canEditHostSettings();
    }

    private static void DrawRectangle(SpriteBatch b, Rectangle bounds, Color color)
    {
        b.Draw(Game1.staminaRect, bounds, color);
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

    private sealed record RowHitArea(PumasiSettingsRow Row, Rectangle Bounds);
}
