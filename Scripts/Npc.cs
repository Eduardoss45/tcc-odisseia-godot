using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Npc : CharacterBody2D
{
    private Node? dialogic;
    private Sprite2D sprite;
    private Node2D player;

    [Export] public bool AiEnabled { get; set; } = false;
    [Export] public float Speed { get; set; } = 80f;
    [Export] public float DetectionRange { get; set; } = 200f;
    [Export] public float StopRange { get; set; } = 32f;
    private Vector2 velocity = Vector2.Zero;

    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/npc_spritesheet.png";
    [Export] public int SpriteSheetRows { get; set; } = 8;
    [Export] public int SpriteSheetCols { get; set; } = 9;
    [Export] public int SpriteSheetWidth { get; set; } = 512;
    [Export] public int SpriteSheetHeight { get; set; } = 576;

    private Rect2[,] animationRects;
    private int animationFrame = 0;
    private float frameTimer = 0f;
    private Queue<int> directionHistory = new Queue<int>();
    private const int MaxHistorySize = 8;

    [Export] public string DialogTimelineName { get; set; } = "npc_dialogo_1";
    [Export] public string CharacterResourcePath { get; set; } = "res://Chars/Npc.dch";
    [Export] public string PlayerResourcePath { get; set; } = "res://Chars/Player.dch";
    private bool isConnected = false;

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
        sprite.Hframes = SpriteSheetCols;
        sprite.Vframes = SpriteSheetRows;

        InitializeAnimationRects();
        SetAnimationFrame(0, 0);

        dialogic = GetNodeOrNull("/root/Dialogic");
        if (dialogic == null)
            GD.PrintErr("Dialogic não encontrado.");

        var clickArea = GetNodeOrNull<Area2D>("Area2D");
        if (clickArea != null)
            clickArea.Connect("input_event", new Callable(this, nameof(OnClicked)));
        else
            GD.PrintErr("Area2D não encontrada no NPC.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");

        if (AiEnabled && player != null)
            UpdateAI();
        else
            velocity = Vector2.Zero;

        Velocity = velocity;
        MoveAndSlide();

        UpdateAnimation(delta);
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
        {
            velocity = (player.Position - Position).Normalized() * Speed;
        }
        else
        {
            velocity = Vector2.Zero;
        }
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
        {
            GD.PrintErr("Frame fora do intervalo da matriz.");
            return;
        }
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

    private int GetMostFrequentDirection()
    {
        if (directionHistory.Count == 0)
            return 0;

        return directionHistory
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First().Key;
    }

    private void OnClicked(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.Pressed &&
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            StartDialog();
        }
    }

    private void StartDialog()
    {
        if (dialogic == null)
        {
            GD.PrintErr("Dialogic node não encontrado!");
            return;
        }

        string timelinePath = $"res://Chars/{DialogTimelineName}.dtl";
        var timelineResource = GD.Load<Resource>(timelinePath);
        if (timelineResource == null)
        {
            GD.PrintErr($"Falha ao carregar timeline: {timelinePath}");
            return;
        }

        var characterResource = GD.Load<Resource>(CharacterResourcePath);
        var playerResource = GD.Load<Resource>(PlayerResourcePath);
        if (characterResource == null || playerResource == null)
        {
            GD.PrintErr("Falha ao carregar personagem .dch");
            return;
        }

        var marker = GetNodeOrNull<Node2D>("Marker2D");
        var playerMarker = GetNode<Node2D>("/root/World/Player/Marker2D");
        if (marker == null || playerMarker == null)
        {
            GD.PrintErr("Marker2D não encontrado.");
            return;
        }

        Variant layoutVariant = dialogic.Call("start", timelineResource);
        CanvasLayer layout = layoutVariant.As<CanvasLayer>();
        if (layout == null)
        {
            GD.PrintErr("Layout retornado não é um CanvasLayer válido.");
            return;
        }

        layout.Call("register_character", characterResource, marker);
        layout.Call("register_character", playerResource, playerMarker);

        if (!isConnected)
        {
            dialogic.Connect("timeline_ended", new Callable(this, nameof(OnDialogFinished)));
            isConnected = true;
        }
    }

    private void OnDialogFinished()
    {
        if (dialogic != null && isConnected)
        {
            dialogic.Disconnect("timeline_ended", new Callable(this, nameof(OnDialogFinished)));
            isConnected = false;
        }
    }

    public void SetAIActive(bool active)
    {
        AiEnabled = active;
    }
}
