using Godot;
using System;

public partial class Npc : CharacterBody2D
{
    private Node? dialogic;
    private bool isConnected = false;

    private Sprite2D sprite;

    [Export]
    public string SpriteSheetPath { get; set; } = "res://Sprites/npc_spritesheet.png";

    [Export]
    public int SpriteSheetRows { get; set; } = 8;

    [Export]
    public int SpriteSheetCols { get; set; } = 9;

    [Export]
    public int SpriteSheetWidth { get; set; } = 512;

    [Export]
    public int SpriteSheetHeight { get; set; } = 576;

    [Export]
    public string DialogTimelineName { get; set; } = "npc_dialogo_1";

    [Export]
    public string CharacterResourcePath { get; set; } = "res://Chars/Npc.dch";

    [Export]
    public string PlayerResourcePath { get; set; } = "res://Chars/Player.dch";

    private Rect2[,] animationRects;

    public override void _Ready()
    {
        dialogic = GetNodeOrNull("/root/Dialogic");
        if (dialogic == null)
        {
            GD.PrintErr("Dialogic não encontrado em /root/Dialogic");
            return;
        }

        sprite = GetNode<Sprite2D>("Sprite2D");

        var texture = GD.Load<Texture2D>(SpriteSheetPath);
        if (texture == null)
        {
            GD.PrintErr($"Falha ao carregar textura: {SpriteSheetPath}");
            return;
        }
        sprite.Texture = texture;

        sprite.RegionEnabled = true;

        InitializeAnimationRects();

        SetAnimationFrame(0, 0);

        Area2D? clickArea = GetNodeOrNull<Area2D>("Area2D");
        if (clickArea != null)
            clickArea.Connect("input_event", new Callable(this, nameof(OnClicked)));
        else
            GD.PrintErr("Area2D não encontrada no NPC.");
    }


    private void InitializeAnimationRects()
    {
        int frameWidth = SpriteSheetWidth / SpriteSheetCols;
        int frameHeight = SpriteSheetHeight / SpriteSheetRows;

        animationRects = new Rect2[SpriteSheetRows, SpriteSheetCols];

        for (int row = 0; row < SpriteSheetRows; row++)
        {
            for (int col = 0; col < SpriteSheetCols; col++)
            {
                animationRects[row, col] = new Rect2(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
            }
        }
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

    public void SetAnimationFrame(int row, int col)
    {
        if (row < 0 || row >= SpriteSheetRows || col < 0 || col >= SpriteSheetCols)
        {
            GD.PrintErr("Frame fora do intervalo da matriz.");
            return;
        }

        sprite.RegionRect = animationRects[row, col];
    }
}
