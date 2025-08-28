using Godot;
using System;

public partial class Hitbox : Node2D
{
    private Polygon2D polygon;
    private bool clicked = false;

    public override void _Ready()
    {
        polygon = GetNode<Polygon2D>("Polygon2D");

        float angle = Mathf.DegToRad(60f) / 2f;
        float radius = 100f;

        Vector2 p0 = Vector2.Zero;
        Vector2 p1 = new Vector2(Mathf.Cos(-angle), Mathf.Sin(-angle)) * radius;
        Vector2 p2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        polygon.Polygon = new Vector2[] { p0, p1, p2 };
        polygon.Visible = false;
    }

    public override void _Process(double delta)
    {
        Vector2 dir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        Rotation = dir.Angle();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("attack"))
        {
            TriggerAttack();
        }
    }

    public async void TriggerAttack()
    {
        if (clicked) return;
        clicked = true;

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        clicked = false;
    }
}
