using Godot;
using System;

public partial class Npc : CharacterBody2D
{
    private Node? dialogic;
    private Sprite2D sprite;
    private Node2D player;

    [Export] public bool AiEnabled { get; set; } = false;
    [Export] public float Speed { get; set; } = 80f;
    [Export] public float DetectionRange { get; set; } = 200f;
    [Export] public float StopRange { get; set; } = 32f;

    // --- DASH CONFIG ---
    [Export] public float DashSpeed { get; set; } = 300f;
    [Export] public float DashCooldown { get; set; } = 2f;
    [Export] public float DashChargeTime { get; set; } = 0.5f;
    [Export] public float StopDistance { get; set; } = 24f;

    private enum DashState { Ready, Charging, Dashing, Cooldown }
    private DashState dashState = DashState.Ready;

    private float dashTimer = 0f;
    private Vector2 dashDir = Vector2.Zero;

    private Vector2 velocity = Vector2.Zero;

    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/npc_spritesheet.png";
    [Export] public int SpriteSheetRows { get; set; } = 8;
    [Export] public int SpriteSheetCols { get; set; } = 9;
    [Export] public int SpriteSheetWidth { get; set; } = 512;
    [Export] public int SpriteSheetHeight { get; set; } = 576;

    private Rect2[,] animationRects;
    private int animationFrame = 0;
    private float frameTimer = 0f;

    [Export] public string DialogTimelineName { get; set; } = "npc_dialogo_1";
    [Export] public string CharacterResourcePath { get; set; } = "res://Chars/Npc.dch";
    [Export] public string PlayerResourcePath { get; set; } = "res://Chars/Player.dch";
    private bool isConnected = false;

    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");
        var texture = GD.Load<Texture2D>(SpriteSheetPath);
        if (texture != null)
        {
            sprite.Texture = texture;
            sprite.Hframes = SpriteSheetCols;
            sprite.Vframes = SpriteSheetRows;
        }

        InitializeAnimationRects();
        SetAnimationFrame(0, 0);

        dialogic = GetNodeOrNull("/root/Dialogic");
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

        HandleDash((float)delta);

        Velocity = velocity;
        MoveAndSlide();
        UpdateAnimation(delta);
    }

    private void HandleDash(float delta)
    {
        dashTimer -= delta;

        switch (dashState)
        {
            case DashState.Ready:
                float dist = Position.DistanceTo(player.Position);
                if (dist <= DetectionRange && dashTimer <= 0f)
                {
                    dashState = DashState.Charging;
                    dashTimer = DashChargeTime;
                    dashDir = (player.Position - Position).Normalized();
                }
                else
                {
                    UpdateAI();
                }
                break;

            case DashState.Charging:
                // Efeito visual: mudar cor enquanto carrega
                float t = 1f - (dashTimer / DashChargeTime);
                sprite.Modulate = new Color(1, 1 - t, 1 - t); // branco -> vermelho

                if (dashTimer <= 0f)
                {
                    dashState = DashState.Dashing;
                    dashTimer = 0.8f; // tempo máximo de dash
                }
                velocity = Vector2.Zero;
                break;

            case DashState.Dashing:
                sprite.Modulate = Colors.White;
                velocity = dashDir * DashSpeed;

                // parar se estiver perto o suficiente do player
                if (Position.DistanceTo(player.Position) <= StopDistance || dashTimer <= 0f)
                {
                    dashState = DashState.Cooldown;
                    dashTimer = DashCooldown;
                    velocity = Vector2.Zero;
                }
                break;

            case DashState.Cooldown:
                UpdateAI();
                if (dashTimer <= 0f)
                {
                    dashState = DashState.Ready;
                }
                break;
        }
    }

    private void UpdateAI()
    {
        if (player == null)
        {
            velocity = Vector2.Zero;
            return;
        }

        float distanceToPlayer = Position.DistanceTo(player.Position);
        float safeStopRange = StopRange + 6f;

        if (distanceToPlayer <= DetectionRange && distanceToPlayer > safeStopRange)
            velocity = (player.Position - Position).Normalized() * Speed;
        else
            velocity = Vector2.Zero;
    }

    private void UpdateAnimation(double delta)
    {
        bool isMoving = velocity.Length() > 0;
        int dirIndex = GetDirectionIndex(velocity);

        if (isMoving)
        {
            frameTimer += (float)delta;
            if (frameTimer >= 0.1f)
            {
                frameTimer = 0f;
                animationFrame = (animationFrame + 1) % SpriteSheetCols;
            }
        }
        else
        {
            animationFrame = 0;
        }

        dirIndex = Math.Clamp(dirIndex, 0, SpriteSheetRows - 1);
        sprite.FrameCoords = new Vector2I(animationFrame, dirIndex);
    }

    private void InitializeAnimationRects()
    {
        int frameWidth = SpriteSheetWidth / SpriteSheetCols;
        int frameHeight = SpriteSheetHeight / SpriteSheetRows;
        animationRects = new Rect2[SpriteSheetRows, SpriteSheetCols];

        for (int row = 0; row < SpriteSheetRows; row++)
            for (int col = 0; col < SpriteSheetCols; col++)
                animationRects[row, col] = new Rect2(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
    }

    public void SetAnimationFrame(int row, int col)
    {
        if (row < 0 || row >= SpriteSheetRows || col < 0 || col >= SpriteSheetCols)
            return;

        sprite.RegionRect = animationRects[row, col];
    }

    private int GetDirectionIndex(Vector2 vel)
    {
        if (vel == Vector2.Zero)
            return 0;

        double angle = Math.Atan2(vel.Y, vel.X);
        double degrees = angle * (180 / Math.PI);
        if (degrees < 0) degrees += 360;

        int sector = (int)Math.Round(degrees / 45.0) % 8;
        int[] spriteMap = { 7, 8, 1, 2, 3, 4, 5, 6 };
        return spriteMap[sector];
    }

    // --- Compat: manter API antiga usada por outros scripts ---
    public void SetAIActive(bool active)
    {
        AiEnabled = active;
    }

    // Se algum script (ex.: NpcAI) ainda usa SetVelocity, mantenha este wrapper.
    // Ele respeita o dash: ignora alterações enquanto estiver Charging/Dashing.
    public void SetVelocity(Vector2 vel)
    {
        if (dashState == DashState.Dashing || dashState == DashState.Charging)
            return;

        velocity = vel;
    }
}
