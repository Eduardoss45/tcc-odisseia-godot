using Godot;
using System;

public partial class Ponte : Node2D
{
    private AnimatedSprite2D animSprite;
    private CollisionShape2D collisionShape;

    [Export] public string PonteSpritePath = "res://Sprites/ponte-spritesheet.png";
    [Export] public int HFrames = 4; // número de frames horizontais

    public override void _Ready()
    {
        animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        collisionShape.Disabled = false;
        animSprite.Visible = true;

        // Cria SpriteFrames dinamicamente
        var spriteFrames = new SpriteFrames();
        var tex = GD.Load<Texture2D>(PonteSpritePath);

        if (tex == null)
        {
            GD.PrintErr($"Não foi possível carregar a textura: {PonteSpritePath}");
            return;
        }

        int frameWidth = (int)(tex.GetSize().X / HFrames);
        int frameHeight = (int)tex.GetSize().Y;

        spriteFrames.AddAnimation("Descer");
        spriteFrames.SetAnimationLoop("Descer", false); // 🔹 necessário para AnimationFinished disparar

        for (int i = 0; i < HFrames; i++)
        {
            var atlas = new AtlasTexture();
            atlas.Atlas = tex;
            atlas.Region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight);
            spriteFrames.AddFrame("Descer", atlas);
        }

        animSprite.SpriteFrames = spriteFrames;
        animSprite.Animation = "Descer";
        animSprite.Frame = 0;

        animSprite.AnimationFinished += OnAnimationFinished;
    }

    public void DescerPonte()
    {
        animSprite.Animation = "Descer";
        animSprite.SpeedScale = 5;
        animSprite.Play();
        GD.Print("Ponte descendo!");
    }

    private void OnAnimationFinished()
    {
        collisionShape.Disabled = true; // jogador pode atravessar
        GD.Print("Ponte abaixada!");
    }
}
