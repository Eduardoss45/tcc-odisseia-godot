using Godot;
using System;

public partial class Mob : CharacterBody2D
{
    private Sprite2D sprite;
    private Node2D player;

    [Export] public float Speed { get; set; } = 200f;
    [Export] public float DetectionRange { get; set; } = 100f;
    [Export] public float StopRange { get; set; } = 32f;
    [Export] public int SpriteSheetRows { get; set; } = 2;
    [Export] public int SpriteSheetCols { get; set; } = 4;
    [Export] public int SpriteSheetWidth { get; set; } = 1600;
    [Export] public int SpriteSheetHeight { get; set; } = 600;
    public void SetAIActive(bool active) => AiEnabled = active;


    // AI
    [Export] public bool AiEnabled { get; set; } = true;

    private Vector2 velocity = Vector2.Zero;

    // Dash
    private DashController dashController;

    // Animação 4x2
    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/mob_spritesheet.png";
    private int frameX = 0;
    private float frameTimer = 0f;
    private int row = 1; // direita por padrão
    private const int Cols = 4;
    private const int Rows = 2;
    [Export] public int FrameWidth { get; set; } = 64;
    [Export] public int FrameHeight { get; set; } = 64;

    // Stats
    [Export] public int MaxHealth { get; set; } = 100;
    private int currentHealth;

    [Export] public float AnimationSpeed { get; set; } = 8f; // frames por segundo

    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");
        var texture = GD.Load<Texture2D>(SpriteSheetPath);
        if (texture == null)
        {
            GD.PrintErr($"Falha ao carregar textura: {SpriteSheetPath}");
            return;
        }

        sprite.Texture = texture;
        sprite.RegionEnabled = true;
        sprite.RegionRect = new Rect2(0, 0, FrameWidth, FrameHeight);

        // Dash
        dashController = new DashController
        {
            DashSpeed = 300f,
            DashChargeTime = 0.5f,
            DashCooldown = 2f,
            StopDistance = 24f,
            DashDuration = 0.8f
        };

        // Vida inicial
        currentHealth = MaxHealth;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");

        if (!AiEnabled || player == null)
        {
            velocity = Vector2.Zero;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }


        // AI simples: seguir player
        float distance = Position.DistanceTo(player.Position);
        if (distance <= DetectionRange && distance > StopRange)
        {
            velocity = (player.Position - Position).Normalized() * Speed;
        }
        else
        {
            velocity = Vector2.Zero;
        }

        // Atualiza dash
        dashController.Update((float)delta, Position, player.Position);
        if (dashController.IsDashing)
            velocity = dashController.Velocity;

        // Movimento
        Velocity = velocity;
        MoveAndSlide();

        // Animação
        UpdateAnimation(delta);
    }

    private void UpdateAnimation(double delta)
    {
        // Prioridade horizontal: decide linha
        if (velocity.X < 0)
            row = 0; // esquerda
        else if (velocity.X > 0)
            row = 1; // direita

        if (velocity.Length() > 0)
        {
            frameTimer += (float)delta;
            if (frameTimer >= 1.0f / AnimationSpeed)
            {
                frameTimer = 0f;
                frameX = (frameX + 1) % Cols;
            }
        }
        else
        {
            frameX = 0;
        }

        sprite.RegionRect = new Rect2(frameX * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
    }

    // --- Sistema de Vida ---
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        QueueFree(); // Remove mob da cena
    }
}
