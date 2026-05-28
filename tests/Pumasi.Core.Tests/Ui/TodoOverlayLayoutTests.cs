using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoOverlayLayoutTests
{
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
