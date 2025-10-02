using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class EntityData
{
    public string ScenePath { get; set; }
    public float[] Position { get; set; }
    public int SpriteSheetRows { get; set; }
    public int SpriteSheetCols { get; set; }
    public int SpriteSheetWidth { get; set; }
    public int SpriteSheetHeight { get; set; }
    public string DialogTimelineName { get; set; }
    public string CharacterResourcePath { get; set; }
    public string PlayerResourcePath { get; set; }
    public string SpriteSheetPath { get; set; }
    public bool AiEnabled { get; set; }
    public float BaseSpeed { get; set; }
    public string PlayerSpriteSheetPath { get; set; }
    public int SpriteHFrames { get; set; }
    public int SpriteVFrames { get; set; }
    public string CrosshairPath { get; set; }
    public string ArrowScenePath { get; set; }
    public string ArrowSpriteSheetPath { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public string AttackSpriteSheetPath { get; set; }
    public int AttackHFrames { get; set; }
    public int AttackVFrames { get; set; }
    public int AttackFrameWidth { get; set; }
    public int AttackFrameHeight { get; set; }
    public string DialogStyle { get; set; } = null;
}

public partial class Game : Node2D
{
    [Export] public string EntitiesConfigPath { get; set; } = "res://Config/entities.json";

    public override void _Ready()
    {
        // 🎵 Música ambiente (apenas MP3)
        var musicPlayer = new AudioStreamPlayer();
        var musicStream = GD.Load<AudioStreamMP3>("res://Musicas/musicaAmbiente.mp3");

        if (musicStream != null)
        {
            musicStream.Loop = true; // 🔁 ativa loop do mp3
            musicPlayer.Stream = musicStream;
            musicPlayer.VolumeDb = -10; // ajuste de volume
            musicPlayer.Bus = "Master";
            AddChild(musicPlayer);
            musicPlayer.Play();
        }
        else
        {
            GD.PrintErr("Não foi possível carregar a música ambiente (MP3).");
        }

        // 📦 Carrega entidades do JSON
        using var file = Godot.FileAccess.Open(EntitiesConfigPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Não foi possível abrir o arquivo JSON: {EntitiesConfigPath}");
            return;
        }

        string jsonText = file.GetAsText();

        try
        {
            var entities = JsonSerializer.Deserialize<List<EntityData>>(jsonText);
            foreach (var data in entities)
                LoadEntityFromData(data);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Erro ao ler JSON: {ex.Message}");
        }

        // ⏱ Timer para iniciar timeline após carregamento
        var timer = new Timer();
        timer.WaitTime = 0.1f;
        timer.OneShot = true;
        AddChild(timer);
        timer.Timeout += OnStartTimeline;
        timer.Start();
    }

    private void OnStartTimeline()
    {
        var dialogicNode = GetNodeOrNull<Node>("/root/Dialogic");
        if (dialogicNode != null)
        {
            GD.Print("Iniciando timeline: res://Chars/npc_dialogo_1.dtl");
            dialogicNode.Call("start", "res://Chars/npc_dialogo_1.dtl");
        }
        else
        {
            GD.PrintErr("Dialogic não encontrado em /root.");
        }
    }

    private void LoadEntityFromData(EntityData data)
    {
        var scene = GD.Load<PackedScene>(data.ScenePath);
        if (scene == null)
        {
            GD.PrintErr($"Falha ao carregar cena: {data.ScenePath}");
            return;
        }

        var instance = scene.Instantiate<Node2D>();

        if (data.Position != null && data.Position.Length == 2)
            instance.Position = new Vector2(data.Position[0], data.Position[1]);

        if (instance is Mob mob)
        {
            mob.SpriteSheetRows = data.SpriteSheetRows;
            mob.SpriteSheetCols = data.SpriteSheetCols;
            mob.SpriteSheetWidth = data.SpriteSheetWidth;
            mob.SpriteSheetHeight = data.SpriteSheetHeight;
            mob.SpriteSheetPath = data.SpriteSheetPath;
            mob.SetAIActive(data.AiEnabled);
        }
        else if (instance is StaticNpc staticNpc)
        {
            staticNpc.SpriteSheetRows = data.SpriteSheetRows;
            staticNpc.SpriteSheetCols = data.SpriteSheetCols;
            staticNpc.DialogTimelineName = data.DialogTimelineName;
            staticNpc.CharacterResourcePath = data.CharacterResourcePath;
            staticNpc.PlayerResourcePath = data.PlayerResourcePath;
            staticNpc.SpriteSheetPath = data.SpriteSheetPath;

            // ⚡ Passa o estilo do diálogo se existir
            if (!string.IsNullOrEmpty(data.DialogStyle))
                staticNpc.SetDialogStyle(data.DialogStyle);
        }
        else if (instance is Player player)
        {
            if (data.BaseSpeed > 0) player.BaseSpeed = data.BaseSpeed;

            if (!string.IsNullOrEmpty(data.PlayerSpriteSheetPath))
            {
                player.SpriteSheetPath = data.PlayerSpriteSheetPath;
                player.SpriteHFrames = data.SpriteHFrames > 0 ? data.SpriteHFrames : 8;
                player.SpriteVFrames = data.SpriteVFrames > 0 ? data.SpriteVFrames : 9;
                player.ApplySprite();
            }

            if (!string.IsNullOrEmpty(data.CrosshairPath))
            {
                player.CrosshairPath = data.CrosshairPath;
                var crossNode = player.GetNodeOrNull<Sprite2D>("Crosshair");
                if (crossNode != null)
                {
                    var crossTex = GD.Load<Texture2D>(player.CrosshairPath);
                    if (crossTex != null)
                        crossNode.Texture = crossTex;
                    crossNode.Visible = false;
                }
            }

            if (!string.IsNullOrEmpty(data.ArrowScenePath))
            {
                var arrowScene = GD.Load<PackedScene>(data.ArrowScenePath);
                if (arrowScene != null)
                {
                    player.ArrowScene = arrowScene;
                    player.ArrowSpriteSheetPath = data.ArrowSpriteSheetPath;
                }
            }

            if (!string.IsNullOrEmpty(data.AttackSpriteSheetPath))
            {
                player.AttackSpriteSheetPath = data.AttackSpriteSheetPath;
                player.AttackHFrames = data.AttackHFrames;
                player.AttackVFrames = data.AttackVFrames;
                player.AttackWidth = data.AttackFrameWidth;
                player.AttackHeight = data.AttackFrameHeight;
            }
        }

        AddChild(instance);
    }
}
