using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoOverlayLayoutTests
{
    [Fact]
    public void CreateIcon_PlacesCompactButtonAwayFromTopHud()
    {
        var icon = TodoOverlayLayout.CreateIcon(viewportWidth: 800, viewportHeight: 600);

        Assert.Equal(24, icon.X);
        Assert.Equal(320, icon.Y);
        Assert.Equal(64, icon.Width);
        Assert.Equal(64, icon.Height);
    }

    [Fact]
    public void TryResolveIconClick_ReturnsTrueOnlyInsideIcon()
    {
        var icon = TodoOverlayLayout.CreateIcon(viewportWidth: 800, viewportHeight: 600);

        Assert.True(TodoOverlayLayout.TryResolveIconClick(icon, icon.X + 1, icon.Y + 1));
        Assert.False(TodoOverlayLayout.TryResolveIconClick(icon, icon.X - 1, icon.Y + 1));
    }

    [Fact]
    public void CreatePopup_PlacesPanelBesideIconWhenThereIsRoom()
    {
        var icon = TodoOverlayLayout.CreateIcon(viewportWidth: 800, viewportHeight: 600);
        var panel = TodoOverlayLayout.CreatePopup(activeTodoCount: 2, lineHeight: 40, viewportWidth: 800, viewportHeight: 600, icon);

        Assert.True(panel.X >= icon.X + icon.Width);
        Assert.Equal(icon.Y, panel.Y);
        Assert.True(panel.X + panel.Width <= 800 - 24);
    }

    [Fact]
    public void Create_StartsBelowTopHudForIdlePanel()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 0, lineHeight: 40, viewportWidth: 800, viewportHeight: 600);

        Assert.Equal(24, panel.X);
        Assert.Equal(320, panel.Y);
        Assert.Equal(2, panel.LineCount);
        Assert.Equal(116, panel.Height);
    }

    [Fact]
    public void Create_ReducesVisibleTodoRowsWhenViewportIsShort()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 8, lineHeight: 40, viewportWidth: 800, viewportHeight: 360);

        Assert.Equal(5, TodoOverlayLayout.GetVisibleTodoCapacity(lineHeight: 40, viewportHeight: 360));
        Assert.Equal(6, panel.LineCount);
        Assert.Equal(276, panel.Height);
        Assert.Equal(60, panel.Y);
        Assert.True(panel.Bottom <= 336);
    }

    [Fact]
    public void CreateReorderControls_PlacesUpAndDownButtonsBesideTodoRows()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 3, lineHeight: 40, viewportWidth: 800, viewportHeight: 600);

        var controls = TodoOverlayLayout.CreateReorderControls(panel, activeTodoCount: 3, lineHeight: 40).ToArray();

        Assert.Equal(4, controls.Length);
        Assert.Contains(controls, control => control.Direction == TodoReorderDirection.Down && control.FromPosition == 1 && control.ToPosition == 2);
        Assert.Contains(controls, control => control.Direction == TodoReorderDirection.Up && control.FromPosition == 2 && control.ToPosition == 1);
        Assert.All(controls, control => Assert.True(control.Bounds.X >= panel.TextX));
        Assert.All(controls, control => Assert.True(control.Bounds.X + control.Bounds.Width <= panel.X + panel.Width - panel.Padding));
    }

    [Fact]
    public void TryResolveReorderClick_ReturnsMoveForButtonHit()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 3, lineHeight: 40, viewportWidth: 800, viewportHeight: 600);
        var controls = TodoOverlayLayout.CreateReorderControls(panel, activeTodoCount: 3, lineHeight: 40);
        var down = controls.Single(control => control.Direction == TodoReorderDirection.Down && control.FromPosition == 1);

        var hit = TodoOverlayLayout.TryResolveReorderClick(controls, down.Bounds.X + 1, down.Bounds.Y + 1, out var move);

        Assert.True(hit);
        Assert.Equal(1, move.FromPosition);
        Assert.Equal(2, move.ToPosition);
    }
}
