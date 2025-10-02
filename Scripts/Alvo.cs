using Godot;
using System;

public partial class Alvo : Node2D
{
    private Sprite2D alvoSprite;
    private Area2D detectArea;

    [Export] public Ponte ponte;

    public override void _Ready()
    {
        // 🔹 Pega automaticamente a ponte pelo caminho
        ponte = GetNode<Ponte>("/root/World/Ponte");
        alvoSprite = GetNode<Sprite2D>("Sprite2D");
        alvoSprite.Texture = GD.Load<Texture2D>("res://Sprites/alvo.png");
        alvoSprite.Scale = new Vector2(1.5f, 1.5f);

        detectArea = GetNode<Area2D>("Area2D");

        detectArea.CollisionLayer = 1 << 1; // alvo na camada 2
        detectArea.CollisionMask = 1 << 0;  // detecta flechas (camada 1)

        detectArea.BodyEntered += OnDetectAreaBodyEntered;
    }

    private void OnDetectAreaBodyEntered(Node2D body)
    {
        if (body is Arrow arrow)
        {
            GD.Print("Alvo foi atingido!");
            ponte?.DescerPonte();
            arrow.QueueFree(); // destrói flecha
            QueueFree();       // destrói alvo
        }
    }
}
