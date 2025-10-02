using Godot;
using System;

public partial class Arrow : CharacterBody2D
{
    [Export] public Vector2 velocity = Vector2.Zero;
    [Export] public string SpritePath = "res://Sprites/arrow.png";

    private Sprite2D sprite;

    public override void _Ready()
    {
        GD.Print("Arrow _Ready chamado");

        sprite = GetNode<Sprite2D>("Sprite2D");
        if (sprite == null)
        {
            GD.PrintErr("Sprite2D não encontrado na Arrow!");
            return;
        }

        if (!string.IsNullOrEmpty(SpritePath))
        {
            var tex = GD.Load<Texture2D>(SpritePath);
            if (tex != null)
                sprite.Texture = tex;
        }

        UpdateRotation();

        // 🔹 Flecha está na camada 1
        CollisionLayer = 1 << 0;
        CollisionMask = 1 << 1; // só detecta alvo ou mobs
    }

    public override void _PhysicsProcess(double delta)
    {
        if (velocity != Vector2.Zero)
        {
            Vector2 motion = velocity * (float)delta;
            var collision = MoveAndCollide(motion);
            if (collision != null)
            {
                GD.Print("Arrow colidiu");

                if (collision.GetCollider() is Mob mob)
                {
                    mob.TakeDamage(5);
                    GD.Print("Mob atingido!");
                }

                if (collision.GetCollider() is Alvo alvo)
                {
                    GD.Print("Alvo atingido por flecha!");
                    alvo.GetParent().RemoveChild(alvo);
                    alvo.QueueFree();
                }

                QueueFree(); // destrói a flecha
            }
        }
    }

    public void UpdateRotation()
    {
        if (velocity != Vector2.Zero)
            Rotation = velocity.Angle() + MathF.PI / 2;
    }
}
