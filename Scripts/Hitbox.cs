using Godot;
using System;

public partial class Hitbox : Node2D
{
    private Polygon2D polygon;
    private Color defaultColor = Colors.White;
    private Color clickColor = Colors.Red;
    private bool clicked = false;

    public override void _Ready()
    {
        polygon = new Polygon2D();
        AddChild(polygon);

        // Define o triângulo/setor de 60° (raio 100 pixels)
        float angle = Mathf.DegToRad(60f) / 2f; // meia abertura
        float radius = 100f;

        Vector2 p0 = Vector2.Zero; // vértice no centro
        Vector2 p1 = new Vector2(Mathf.Cos(-angle), Mathf.Sin(-angle)) * radius;
        Vector2 p2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        polygon.Polygon = new Vector2[] { p0, p1, p2 };
        polygon.Color = defaultColor;
    }

    public override void _Process(double delta)
    {
        // Sempre olha para o mouse
        Vector2 dir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        Rotation = dir.Angle();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            // Muda cor ao clicar
            if (!clicked)
                StartClickEffect();
        }
    }

    private async void StartClickEffect()
    {
        clicked = true;
        polygon.Color = clickColor;

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout"); // muda por 0.2s

        polygon.Color = defaultColor;
        clicked = false;
    }
}
