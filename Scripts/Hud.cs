using Godot;
using System;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
    private TextureRect lifeIcon;
    private TextureRect energyIcon;

    private List<Texture2D> lifeStages = new List<Texture2D>();
    private List<Texture2D> energyStages = new List<Texture2D>();

    [ExportCategory("Life Spritesheet")]
    [Export] private string lifeSpriteSheetPath = "res://Sprites/vida_partial_spritesheet.png";
    [Export] private int lifeFrames = 11;           // Vida tem 11 frames (ex.: 0..10)
    [Export] private int lifeFrameWidth = 480;
    [Export] private int lifeFrameHeight = 160;
    [Export] private bool lifeEmptyToFull = true;    // true se o spritesheet for vazio->cheio

    [ExportCategory("Energy Spritesheet")]
    [Export] private string energySpriteSheetPath = "res://Sprites/energia_partial_spritesheet.png";
    [Export] private int energyFrames = 9;           // ⚠️ Energia tem 9 estágios (0..8)
    [Export] private int energyFrameWidth = 480;
    [Export] private int energyFrameHeight = 160;
    [Export] private bool energyEmptyToFull = true;  // true se o spritesheet for vazio->cheio

    // Quando energia == 0, alguns spritesheets deixam o último frame 100% transparente.
    // Se quiser garantir que apareça uma barra “zerada” visível, use o penúltimo frame:
    [ExportCategory("Energy Display")]
    [Export] private bool useVisibleEmptyForEnergy = true;

    public override void _Ready()
    {
        lifeIcon = GetNode<TextureRect>("MarginContainer/VBoxContainer/Life");
        energyIcon = GetNode<TextureRect>("MarginContainer/VBoxContainer/Energy");

        lifeStages = LoadStages(lifeSpriteSheetPath, lifeFrames, lifeFrameWidth, lifeFrameHeight);
        energyStages = LoadStages(energySpriteSheetPath, energyFrames, energyFrameWidth, energyFrameHeight);

        // Começam cheias visualmente
        UpdateLife(lifeStages.Count - 1);
        UpdateEnergy(energyStages.Count - 1);
    }

    private List<Texture2D> LoadStages(string path, int frames, int frameWidth, int frameHeight)
    {
        var stages = new List<Texture2D>();

        var sheet = GD.Load<Texture2D>(path);
        if (sheet == null)
        {
            GD.PrintErr("Spritesheet não encontrado: ", path);
            return stages;
        }

        for (int i = 0; i < frames; i++)
        {
            var region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight);
            var atlas = new AtlasTexture { Atlas = sheet, Region = region };
            stages.Add(atlas);
        }

        return stages;
    }

    // Mantive a API antiga (índice direto), útil para vida
    public void UpdateLife(int stage)
    {
        if (lifeStages.Count == 0) return;

        stage = Mathf.Clamp(stage, 0, lifeStages.Count - 1);
        if (lifeEmptyToFull)
            stage = (lifeStages.Count - 1) - stage;

        lifeIcon.Texture = lifeStages[stage];
    }

    // Mantive também a API antiga da energia (índice direto),
    // mas recomendo usar UpdateEnergyByValue para mapear corretamente.
    public void UpdateEnergy(int stage)
    {
        if (energyStages.Count == 0) return;

        stage = Mathf.Clamp(stage, 0, energyStages.Count - 1);
        if (energyEmptyToFull)
            stage = (energyStages.Count - 1) - stage;

        // Se o último frame for transparente e a energia estiver em zero,
        // forçamos um frame visível “zerado”
        if (useVisibleEmptyForEnergy && stage == energyStages.Count - 1 && energyStages.Count >= 2)
            stage = energyStages.Count - 2;

        energyIcon.Texture = energyStages[stage];
    }

    // ✅ Nova API recomendada — mapeia valor atual/max para o frame correto
    public void UpdateEnergyByValue(float current, float max)
    {
        if (energyStages.Count == 0 || max <= 0f) return;

        float t = Mathf.Clamp(current / max, 0f, 1f);
        int stage = Mathf.RoundToInt(t * (energyStages.Count - 1));

        if (energyEmptyToFull)
            stage = (energyStages.Count - 1) - stage;

        if (useVisibleEmptyForEnergy && current <= 0f && energyStages.Count >= 2)
            stage = energyStages.Count - 2;

        energyIcon.Texture = energyStages[stage];
    }
}
