using Godot;
using System;

public partial class Ponte : Node2D
{
    private AnimatedSprite2D animSprite;
    private CollisionShape2D collisionShape;
    private Area2D area;

    public override void _Ready()
    {
        animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        var staticBody = GetNode<StaticBody2D>("StaticBody2D");
        collisionShape = staticBody.GetNode<CollisionShape2D>("CollisionShape2D");
        area = GetNode<Area2D>("Area2D");

        // Ponte começa sólida
        collisionShape.Disabled = false;

        // Conecta sinais de entrada/saída do player
        area.BodyEntered += OnBodyEntered;
        area.BodyExited += OnBodyExited;

        // 🔍 Listar animações para garantir que "Descer" existe
        foreach (var anim in animSprite.SpriteFrames.GetAnimationNames())
        {
            GD.Print("Animação encontrada: " + anim);
        }

        // Ponte começa travada no primeiro frame da animação "Descer"
        animSprite.Animation = "Descer"; // <-- aqui estava "default"
        animSprite.Stop();
        animSprite.Frame = 0;

        // Conecta o sinal de fim da animação
        animSprite.AnimationFinished += OnAnimationFinished;
    }

    public void DescerPonte()
    {
        if (!animSprite.IsPlaying())
        {
            animSprite.SpeedScale = 5;
            animSprite.Play("Descer"); // <-- aqui também estava "default"
        }
    }

    private void OnAnimationFinished()
    {
        // Após a animação, a ponte se torna atravessável
        collisionShape.Disabled = true;
    }

    private void OnBodyEntered(Node body)
    {
        if (body.Name == "Player")
            body.Call("DesativarColisao");
    }

    private void OnBodyExited(Node body)
    {
        if (body.Name == "Player")
            body.Call("AtivarColisao");
    }
}
