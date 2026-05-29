using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pumasi.Core.Configuration;
using Pumasi.Core.Ui;
using StardewValley;
using StardewValley.Menus;

namespace Pumasi.UI;

internal sealed class PumasiSettingsPage : IClickableMenu
{
    private static readonly Color TextColor = new(86, 22, 12);
    private static readonly Color MutedTextColor = new(126, 81, 47);
    private static readonly Color CheckboxBorder = new(92, 47, 19);
    private static readonly Color CheckboxFill = new(242, 180, 91);
    private static readonly Color CheckboxEnabledFill = new(76, 181, 63);
    private static readonly Color CheckboxDisabledFill = new(132, 105, 75);
    private static readonly Color LanguageChipFill = new(63, 142, 184);

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
        var layout = PumasiSettingsPageLayoutFactory.Create(xPositionOnScreen, yPositionOnScreen, width, height, rows.Count);
        DrawTitle(b, layout);

        for (var i = 0; i < rows.Count && i < layout.VisibleRows; i++)
        {
            var row = rows[i];
            var rowY = layout.FirstRowY + i * layout.RowHeight;
            var checkboxBounds = new Rectangle(layout.CheckboxX, rowY + 10, layout.CheckboxSize, layout.CheckboxSize);
            var rowBounds = new Rectangle(layout.ContentX, rowY, layout.ContentRight - layout.ContentX, layout.RowHeight);
            var enabled = GetValue(row.Key);
            var editable = CanEdit(row.Key);

            if (row.Key == PumasiSettingsKey.Language)
                DrawLanguageChip(b, checkboxBounds, config.Ui.Language);
            else
                DrawCheckbox(b, checkboxBounds, enabled, editable);

            DrawRowLabel(b, row, config.Ui.Language, new Vector2(layout.RowLabelX, rowY + 6), layout.RowLabelMaxWidth, editable);
            rowHitAreas.Add(new RowHitArea(row, rowBounds));
        }

        DrawFooter(b, layout);
    }

    private void DrawTitle(SpriteBatch b, PumasiSettingsPageLayout layout)
    {
        var title = PumasiText.Get(config.Ui.Language, PumasiTextKey.SettingsTitle);
        var subtitle = canEditHostSettings()
            ? PumasiText.Get(config.Ui.Language, PumasiTextKey.SettingsHostSubtitle)
            : PumasiText.Get(config.Ui.Language, PumasiTextKey.SettingsGuestSubtitle);
        var titlePosition = new Vector2(layout.ContentX, layout.TitleY);
        var subtitlePosition = new Vector2(layout.ContentX, layout.SubtitleY);

        b.DrawString(Game1.dialogueFont, title, titlePosition + new Vector2(2, 2), Color.White * 0.55f);
        b.DrawString(Game1.dialogueFont, title, titlePosition, TextColor);
        b.DrawString(Game1.smallFont, subtitle, subtitlePosition + new Vector2(1, 1), Color.White * 0.45f);
        b.DrawString(Game1.smallFont, subtitle, subtitlePosition, MutedTextColor);
    }

    private void DrawFooter(SpriteBatch b, PumasiSettingsPageLayout layout)
    {
        var text = PumasiText.Get(config.Ui.Language, PumasiTextKey.SettingsFooter);
        var position = new Vector2(layout.ContentX, layout.FooterY);
        var clipped = TrimToWidth(text, layout.FooterMaxWidth);
        b.DrawString(Game1.smallFont, clipped, position + new Vector2(1, 1), Color.White * 0.45f);
        b.DrawString(Game1.smallFont, clipped, position, MutedTextColor);
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

    private static void DrawLanguageChip(SpriteBatch b, Rectangle bounds, UiLanguage language)
    {
        DrawRectangle(b, bounds, CheckboxBorder);
        DrawRectangle(b, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8), LanguageChipFill);

        var mark = language == UiLanguage.English ? "EN" : "KO";
        var markSize = Game1.tinyFont.MeasureString(mark);
        var markPosition = new Vector2(
            bounds.X + (bounds.Width - markSize.X) / 2f,
            bounds.Y + (bounds.Height - markSize.Y) / 2f);
        b.DrawString(Game1.tinyFont, mark, markPosition + new Vector2(1, 1), Color.Black * 0.35f);
        b.DrawString(Game1.tinyFont, mark, markPosition, Color.White);
    }

    private static void DrawRowLabel(SpriteBatch b, PumasiSettingsRow row, UiLanguage language, Vector2 position, int maxWidth, bool editable)
    {
        var color = editable ? TextColor : MutedTextColor;
        var label = row.Key == PumasiSettingsKey.Language
            ? $"{row.FormatLabel(language)}: {PumasiText.GetLanguageName(language, language)}"
            : row.FormatLabel(language);
        label = TrimToWidth(label, maxWidth);
        b.DrawString(Game1.smallFont, label, position + new Vector2(1, 1), Color.White * 0.4f);
        b.DrawString(Game1.smallFont, label, position, color);
    }

    private bool GetValue(PumasiSettingsKey key)
    {
        return key switch
        {
            PumasiSettingsKey.Language => config.Ui.Language == UiLanguage.Korean,
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
            case PumasiSettingsKey.Language:
                config.Ui.Language = config.Ui.Language == UiLanguage.English ? UiLanguage.Korean : UiLanguage.English;
                break;

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
        if (key is PumasiSettingsKey.Language or PumasiSettingsKey.ShowTodoOverlay or PumasiSettingsKey.ShowHelperStatusNotifications)
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
