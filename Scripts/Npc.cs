using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Npc : CharacterBody2D
{
    // -------------------
    // NODES
    // -------------------
    private Node? dialogic;
    private Sprite2D sprite;
    private Node2D player;

    // -------------------
    // AI CONFIG
    // -------------------
    [Export] public bool AiEnabled { get; set; } = false;
    [Export] public float Speed { get; set; } = 80f;
    [Export] public float DetectionRange { get; set; } = 200f;
    [Export] public float StopRange { get; set; } = 32f;
    private Vector2 velocity = Vector2.Zero;

    // -------------------
    // SPRITE / ANIMATION
    // -------------------
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

    // -------------------
    // DIALOG
    // -------------------
    [Export] public string DialogTimelineName { get; set; } = "npc_dialogo_1";
    [Export] public string CharacterResourcePath { get; set; } = "res://Chars/Npc.dch";
    [Export] public string PlayerResourcePath { get; set; } = "res://Chars/Player.dch";

    private bool isConnected = false;

    // -------------------
    // GODOT CALLBACKS
    // -------------------
    public override void _Ready()
    {
        // Player
        player = GetNodeOrNull<Node2D>("/root/World/Player");
        if (player == null)
            GD.PrintErr("Player não encontrado em /root/World/Player");

        // Dialogic
        dialogic = GetNodeOrNull("/root/Dialogic");
        if (dialogic == null)
        {
            GD.PrintErr("Dialogic não encontrado em /root/Dialogic");
            return;
        }

        // Sprite
        sprite = GetNode<Sprite2D>("Sprite2D");
        var texture = GD.Load<Texture2D>(SpriteSheetPath);
        if (texture == null)
        {
            GD.PrintErr($"Falha ao carregar textura: {SpriteSheetPath}");
            return;
        }
        sprite.Texture = texture;
        sprite.RegionEnabled = false;
        InitializeAnimationRects();
        SetAnimationFrame(0, 0);

        sprite.Hframes = SpriteSheetCols; // número de colunas
        sprite.Vframes = SpriteSheetRows; // número de linhas

        // Clique
        Area2D? clickArea = GetNodeOrNull<Area2D>("Area2D");
        if (clickArea != null)
            clickArea.Connect("input_event", new Callable(this, nameof(OnClicked)));
        else
            GD.PrintErr("Area2D não encontrada no NPC.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!AiEnabled || player == null || sprite == null)
            return;

        UpdateAI();
        MoveAndSlide();
        UpdateAnimation(delta);
    }


    // -------------------
    // AI / MOVIMENTO
    // -------------------
    private void UpdateAI()
    {
        if (player == null)
        {
            velocity = Vector2.Zero;
            return;
        }

        float distanceToPlayer = Position.DistanceTo(player.Position);

        if (distanceToPlayer <= DetectionRange && distanceToPlayer > StopRange)
            velocity = (player.Position - Position).Normalized() * Speed;
        else
            velocity = Vector2.Zero;

        // Atualiza o CharacterBody2D
        Velocity = velocity;
    }


    // -------------------
    // ANIMAÇÃO
    // -------------------
    private void UpdateAnimation(double delta)
    {
        velocity = Velocity;
        bool isMoving = velocity.Length() > 0;

        int dirIndex = isMoving ? GetDirectionIndex(velocity) : GetMostFrequentDirection();
        dirIndex = Math.Clamp(dirIndex, 0, SpriteSheetRows - 1);

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

    private int GetDirectionIndex(Vector2 velocity)
    {
        if (velocity == Vector2.Zero)
            return 0;

        double angle = Math.Atan2(velocity.Y, velocity.X);
        double degrees = angle * (180 / Math.PI);
        
        if (degrees < 0)
            degrees += 360;

        int sector = (int)Math.Round(degrees / 45.0) % 8;
        int[] spriteMap = {
        7, // 0 = direita
        8, // 1 = baixo-direita
        1, // 2 = baixo
        2, // 3 = baixo-esquerda
        3, // 4 = esquerda
        4, // 5 = cima-esquerda
        5, // 6 = cima
        6  // 7 = cima-direita
    };

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

    // -------------------
    // DIÁLOGO
    // -------------------
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
        Resource? timelineResource = GD.Load<Resource>(timelinePath);
        if (timelineResource == null)
        {
            GD.PrintErr($"Falha ao carregar timeline: {timelinePath}");
            return;
        }

        Resource? characterResource = GD.Load<Resource>(CharacterResourcePath);
        Resource? playerResource = GD.Load<Resource>(PlayerResourcePath);
        if (characterResource == null || playerResource == null)
        {
            GD.PrintErr("Falha ao carregar personagem .dch");
            return;
        }

        Node2D? marker = GetNodeOrNull<Node2D>("Marker2D");
        Node2D? playerMarker = GetNode<Node2D>("/root/World/Player/Marker2D");
        if (marker == null || playerMarker == null)
        {
            GD.PrintErr("Marker2D não encontrado.");
            return;
        }

        Variant layoutVariant = dialogic.Call("start", timelineResource);
        CanvasLayer? layout = layoutVariant.As<CanvasLayer>();
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

    // -------------------
    // UTILITÁRIOS
    // -------------------
    public void SetAIActive(bool active)
    {
        AiEnabled = active;
    }
}
