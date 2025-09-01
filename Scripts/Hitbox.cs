using Godot;
using System;

public partial class Hitbox : Node2D
{

    private bool clicked = false;
    private Polygon2D polygon;
    private CollisionPolygon2D collision;

    public override void _Ready()
    {
        polygon = GetNode<Polygon2D>("Polygon2D");
        collision = GetNode<CollisionPolygon2D>("Area2D/CollisionPolygon2D");

        float angle = Mathf.DegToRad(60f) / 2f;
        float radius = 100f;

        Vector2 p0 = Vector2.Zero;
        Vector2 p1 = new Vector2(Mathf.Cos(-angle), Mathf.Sin(-angle)) * radius;
        Vector2 p2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        polygon.Polygon = new Vector2[] { p0, p1, p2 };
        collision.Polygon = new Vector2[] { p0, p1, p2 };

        polygon.Visible = false; // nunca aparece
        collision.Disabled = true;

        var area = GetNode<Area2D>("Area2D");
        area.BodyEntered += OnBodyEntered;
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

        // Ativa somente a colisão (sem visual)
        collision.Disabled = false;

        // tempo ativo (0.2s no seu caso)
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        // Desativa hitbox de novo
        collision.Disabled = true;

        clicked = false;
    }


    private void OnBodyEntered(Node body)
    {
        if (body is Mob mob)
        {
            mob.TakeDamage(10);
        }
    }
}

