using Godot;
using System;

public partial class StaticNpc : CharacterBody2D
{
    private Node? dialogic;
    private Sprite2D sprite;
    private Player player;

    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/Staticnpc_spritesheet.png";
    [Export] public int SpriteSheetRows { get; set; } = 8; // Vframes
    [Export] public int SpriteSheetCols { get; set; } = 9; // Hframes

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
        if (texture == null)
        {
            GD.PrintErr($"Falha ao carregar textura: {SpriteSheetPath}");
            return;
        }

        sprite.Texture = texture;
        sprite.Hframes = SpriteSheetCols; // divide em colunas
        sprite.Vframes = SpriteSheetRows; // divide em linhas

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

    public void SetAnimationFrame(int row, int col)
    {
        if (row < 0 || row >= SpriteSheetRows || col < 0 || col >= SpriteSheetCols)
        {
            GD.PrintErr("Frame fora do intervalo da matriz.");
            return;
        }

        int frameIndex = (row * SpriteSheetCols) + col;
        sprite.Frame = frameIndex;
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
        // Bloqueia ataque do player
        if (player != null)
            player.CanAttack = false;

        if (dialogic == null)
        {
            GD.PrintErr("Dialogic node não encontrado!");
            return;
        }

        SetAnimationFrame(0, 1);

        // Carrega a timeline
        string timelinePath = $"res://Chars/{DialogTimelineName}.dtl";
        Resource? timelineResource = GD.Load<Resource>(timelinePath);
        if (timelineResource == null)
        {
            GD.PrintErr($"Falha ao carregar timeline: {timelinePath}");
            return;
        }

        // ⚡ Aplica o tema do diálogo antes de iniciar
        if (!string.IsNullOrEmpty(dialogStyle))
        {
            Resource? theme = dialogStyle switch
            {
                "Balloon" => GD.Load<Resource>("res://DialogicThemes/BalloonTheme.tres"),
                _ => null
            };

            if (theme != null && dialogic != null)
                dialogic.Set("theme", theme);
        }

        // Carrega personagens
        var characterResource = GD.Load<Resource>(CharacterResourcePath);
        var playerResource = GD.Load<Resource>(PlayerResourcePath);
        if (characterResource == null || playerResource == null)
        {
            GD.PrintErr("Falha ao carregar personagem .dch");
            return;
        }

        // Pegando markers
        var marker = GetNodeOrNull<Node2D>("Marker2D");
        var playerMarker = GetNode<Node2D>("/root/World/Player/Marker2D");
        if (marker == null || playerMarker == null)
        {
            GD.PrintErr("Marker2D não encontrado.");
            return;
        }

        // Inicia diálogo
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

        // Libera ataque do player
        if (player != null)
            player.CanAttack = true;

        SetAnimationFrame(0, 0);

        if (dialogic != null && isConnected)
        {
            dialogic.Disconnect("timeline_ended", new Callable(this, nameof(OnDialogFinished)));
            isConnected = false;
        }
        SetAnimationFrame(0, 0);
    }

    private string dialogStyle = null;

    public void SetDialogStyle(string style)
    {
        dialogStyle = style; // guarda para usar quando iniciar o diálogo
    }

}
