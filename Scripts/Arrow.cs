using Godot;
using System;

public partial class Arrow : CharacterBody2D
{
    [Export] public Vector2 velocity = Vector2.Zero;
    [Export] public string SpritePath;

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
        else
        {
            GD.Print("Sprite2D encontrado");
        }

        if (!string.IsNullOrEmpty(SpritePath))
        {
            GD.Print($"Tentando carregar textura: {SpritePath}");
            var tex = GD.Load<Texture2D>(SpritePath);
            if (tex != null)
            {
                sprite.Texture = tex;
                GD.Print("Textura aplicada com sucesso");
            }
            else
            {
                GD.PrintErr($"Falha ao carregar textura: {SpritePath}");
            }
        }

        UpdateRotation();

        CollisionLayer = 1 << 0;
        CollisionMask = 1 << 1;

        GD.Print("Arrow inicializada. Velocity: ", velocity);
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
                    GD.Print("Mob atingido! Dano: 5");
                }

                QueueFree();
                return;
            }
        }
    }


    public void UpdateRotation()
    {
        if (velocity != Vector2.Zero)
        {
            Rotation = velocity.Angle() + MathF.PI / 2;
            GD.Print("Arrow rotação aplicada: ", Rotation);
        }
    }

    public void ApplySprite()
    {
        if (sprite != null && !string.IsNullOrEmpty(SpritePath))
        {
            GD.Print($"ApplySprite chamado. Carregando: {SpritePath}");
            var tex = GD.Load<Texture2D>(SpritePath);
            if (tex != null)
            {
                sprite.Texture = tex;
                GD.Print("Textura aplicada via ApplySprite");
            }
            else
            {
                GD.PrintErr($"Falha ao carregar textura via ApplySprite: {SpritePath}");
            }
        }
    }
}
