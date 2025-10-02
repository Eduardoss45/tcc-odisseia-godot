using Godot;
using System;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
    private AnimatedSprite2D lifeIcon;
    private AnimatedSprite2D energyIcon;

    private List<Texture2D> lifeStages = new();
    private List<Texture2D> energyStages = new();

    [ExportCategory("Life Spritesheet")]
    [Export] private string lifeSpriteSheetPath = "res://Sprites/vida_partial_spritesheet.png";
    [Export] private int lifeFrames = 11;
    [Export] private int lifeFrameWidth = 480;
    [Export] private int lifeFrameHeight = 160;
    [Export] private bool lifeEmptyToFull = true;

    [ExportCategory("Life Full Animation")]
    [Export] private SpriteFrames lifeFullSpriteFrames;

    [ExportCategory("Energy Spritesheet")]
    [Export] private string energySpriteSheetPath = "res://Sprites/energia_partial_spritesheet.png";
    [Export] private int energyFrames = 9;
    [Export] private int energyFrameWidth = 480;
    [Export] private int energyFrameHeight = 160;
    [Export] private bool energyEmptyToFull = true;

    [ExportCategory("Energy Full Animation")]
    [Export] private SpriteFrames energyFullSpriteFrames;

    [ExportCategory("Energy Display")]
    [Export] private bool useVisibleEmptyForEnergy = true;

    public override void _Ready()
    {
        lifeIcon = GetNode<AnimatedSprite2D>("MarginContainer/VBoxContainer/Life");
        energyIcon = GetNode<AnimatedSprite2D>("MarginContainer/VBoxContainer/Energy");

        lifeStages = LoadStages(lifeSpriteSheetPath, lifeFrames, lifeFrameWidth, lifeFrameHeight);
        energyStages = LoadStages(energySpriteSheetPath, energyFrames, energyFrameWidth, energyFrameHeight);

        UpdateLife(lifeStages.Count - 1);
        UpdateEnergy(energyStages.Count - 1);
        energyIcon.SpriteFrames = energyFullSpriteFrames;
        energyIcon.Play("fullEnergy");
        GD.Print("Tentando iniciar animação fullEnergy");

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

    public void UpdateLife(int stage)
    {
        if (lifeStages.Count == 0) return;

        stage = Mathf.Clamp(stage, 0, lifeStages.Count - 1);
        bool isFull = stage == lifeStages.Count - 1;

        if (lifeEmptyToFull)
            stage = (lifeStages.Count - 1) - stage;

        if (isFull && lifeFullSpriteFrames != null)
        {
            lifeIcon.SpriteFrames = lifeFullSpriteFrames;
            lifeIcon.Play("fullLife");
        }
        else
        {
            var tempFrames = new SpriteFrames();
            tempFrames.AddAnimation("static");
            tempFrames.SetAnimationLoop("static", false);
            tempFrames.AddFrame("static", lifeStages[stage]);

            lifeIcon.SpriteFrames = tempFrames;
            lifeIcon.Play("static");
        }
    }

    public void UpdateEnergy(int stage)
    {
        if (energyStages.Count == 0) return;

        stage = Mathf.Clamp(stage, 0, energyStages.Count - 1);

        // Verifica se está cheio antes da inversão
        bool isFull = !energyEmptyToFull
            ? stage == energyStages.Count - 1
            : stage == 0;

        if (energyEmptyToFull)
            stage = (energyStages.Count - 1) - stage;

        SetEnergyIcon(stage, isFull);
    }

    public void UpdateEnergyByValue(float current, float max)
    {
        if (energyStages.Count == 0 || max <= 0f) return;

        float t = Mathf.Clamp(current / max, 0f, 1f);
        int rawStage = Mathf.RoundToInt(t * (energyStages.Count - 1));

        int stage = energyEmptyToFull ? (energyStages.Count - 1) - rawStage : rawStage;

        if (useVisibleEmptyForEnergy && current <= 0f && energyStages.Count >= 2)
            stage = energyStages.Count - 2;

        bool isFull = rawStage == energyStages.Count - 1;

        SetEnergyIcon(stage, isFull);
    }

    private void SetEnergyIcon(int stage, bool isFull)
    {
        if (isFull && energyFullSpriteFrames != null)
        {
            energyIcon.SpriteFrames = energyFullSpriteFrames;
            energyIcon.Play("fullEnergy");
        }
        else
        {
            var tempFrames = new SpriteFrames();
            tempFrames.AddAnimation("static");
            tempFrames.SetAnimationLoop("static", false);
            tempFrames.AddFrame("static", energyStages[stage]);

            energyIcon.SpriteFrames = tempFrames;
            energyIcon.Play("static");
        }
    }

}
