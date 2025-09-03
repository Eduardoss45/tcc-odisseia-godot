using Godot;
using System;

public partial class Mob : CharacterBody2D
{
    private Sprite2D sprite;
    private Node2D player;

    [Export] public float Speed { get; set; } = 125f;
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

    // --- Animacao de Movimento 4x2 ---
    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/mob_spritesheet.png";
    private int frameX = 0;
    private float frameTimer = 0f;
    private int row = 0; // começa virado para esquerda
    private const int Cols = 4;
    private const int Rows = 2;
    [Export] public int FrameWidth { get; set; } = 64;
    [Export] public int FrameHeight { get; set; } = 64;

    [Export] public float AnimationSpeed { get; set; } = 4f; // frames por segundo
    private int lastRow = 1; // começa virado para esquerda

    // --- Animacao Parado ---
    [Export] public string IdleSpriteSheetPath { get; set; } = "res://Sprites/javali_parado_spritesheet.png";
    [Export] public int IdleFrameWidth { get; set; } = 320;
    [Export] public int IdleFrameHeight { get; set; } = 300;
    private int idleFrameX = 0;
    private float idleFrameTimer = 0f;
    private int idleCols = 2;
    private int idleRows = 2;

    // Stats
    [Export] public int MaxHealth { get; set; } = 100;
    private int currentHealth;

    private bool canAttack = true;
    private Timer attackCooldown;
    private float attackCooldownTime = 1f;
    private float attackTimer = 0f;

    // --- Animacao de Morte ---
    private bool isDying = false;
    [Export] public string DeathSpriteSheetPath { get; set; } = "res://Sprites/javali_abatido_spritesheet.png";
    [Export] public int DeathFrameWidth { get; set; } = 400;
    [Export] public int DeathFrameHeight { get; set; } = 300;

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
        sprite.Scale = new Vector2(0.3f, 0.3f);

        // Dash
        dashController = new DashController
        {
            DashSpeed = 300f,
            DashChargeTime = 0.5f,
            DashCooldown = 2f,
            StopDistance = 24f,
            DashDuration = 0.8f
        };

        base._Ready();

        // Hitbox
        var hitbox = GetNode<Area2D>("Hitbox");
        hitbox.BodyEntered += OnHitPlayer;

        // Timer de cooldown
        attackCooldown = new Timer();
        attackCooldown.WaitTime = 1f;
        attackCooldown.OneShot = true;
        attackCooldown.Autostart = false;
        AddChild(attackCooldown);
        attackCooldown.Timeout += () => { canAttack = true; };

        currentHealth = MaxHealth;

        // Começa virado para direita
        row = 0;
        lastRow = 0;
    }

    private void OnHitPlayer(Node body)
    {
        if (body is Player player && canAttack)
        {
            player.TakeDamage(1);
            GD.Print("Dano aplicado ao Player!");

            canAttack = false;
            attackCooldown.Start();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isDying) return;

        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");

        if (!AiEnabled || player == null)
        {
            velocity = Vector2.Zero;
            Velocity = velocity;
            MoveAndSlide();
            UpdateIdleAnimation(delta);
            return;
        }

        attackTimer -= (float)delta;

        var hitbox = GetNode<Area2D>("Hitbox");
        foreach (var body in hitbox.GetOverlappingBodies())
        {
            if (body is Player player && attackTimer <= 0f)
            {
                player.TakeDamage(1);
                GD.Print("Dano aplicado ao Player!");
                attackTimer = attackCooldownTime;
            }
        }

        float distance = Position.DistanceTo(player.Position);

        if (distance <= DetectionRange && distance > StopRange)
            velocity = (player.Position - Position).Normalized() * Speed;
        else
            velocity = Vector2.Zero;

        dashController.Update((float)delta, Position, player.Position);
        if (dashController.IsDashing)
            velocity = dashController.Velocity;

        Velocity = velocity;
        MoveAndSlide();

        // Animação
        if (velocity.Length() > 2f || dashController.IsDashing)
            UpdateMovementAnimation(delta);
        else
            UpdateIdleAnimation(delta);
    }

    private void UpdateMovementAnimation(double delta)
    {
        if (player == null) return;

        if (velocity.X < -1)
            row = 0; // esquerda
        else if (velocity.X > 1)
            row = 1; // direita
        else
            row = lastRow;

        lastRow = row;

        frameTimer += (float)delta;
        if (frameTimer >= 1.0f / AnimationSpeed)
        {
            frameTimer -= 1.0f / AnimationSpeed;
            frameX = (frameX + 1) % Cols;
        }

        sprite.Texture = GD.Load<Texture2D>(SpriteSheetPath);
        sprite.RegionRect = new Rect2(frameX * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
        sprite.Scale = new Vector2(1f, 1f);
    }

    private void UpdateIdleAnimation(double delta)
    {
        if (currentHealth <= 0 || isDying) return;

        int idleRow = (lastRow == 0) ? 1 : 0; // direita = 0, esquerda = 1

        idleFrameTimer += (float)delta;
        if (idleFrameTimer >= 1.0f / AnimationSpeed)
        {
            idleFrameTimer -= 1.0f / AnimationSpeed;
            idleFrameX = (idleFrameX + 1) % idleCols;
        }

        sprite.Texture = GD.Load<Texture2D>(IdleSpriteSheetPath);
        sprite.RegionRect = new Rect2(idleFrameX * IdleFrameWidth, idleRow * IdleFrameHeight, IdleFrameWidth, IdleFrameHeight);
        sprite.Scale = new Vector2(0.3f, 0.3f);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        GD.Print($"Mob levou {amount} de dano! HP: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        var deathTexture = GD.Load<Texture2D>(DeathSpriteSheetPath);
        if (deathTexture == null)
        {
            GD.PrintErr($"Falha ao carregar textura de morte: {DeathSpriteSheetPath}");
            QueueFree();
            return;
        }

        sprite.Texture = deathTexture;
        sprite.RegionEnabled = true;

        int deathFrameIndex = (lastRow == 0) ? 1 : 0;
        sprite.RegionRect = new Rect2(deathFrameIndex * DeathFrameWidth, 0, DeathFrameWidth, DeathFrameHeight);
        sprite.Scale = new Vector2(0.3f, 0.3f);

        AiEnabled = false;

        var hitbox = GetNode<Area2D>("Hitbox");
        hitbox.SetDeferred("monitoring", false);
    }
}
