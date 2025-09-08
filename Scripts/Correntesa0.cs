using Godot;
using System;

public partial class rio : Sprite2D
{
    public override void _Ready()
    {
        AdicionarColisao(this);

        foreach (Node child in GetChildren())
        {
            if (child is Sprite2D spriteChild)
            {
                AdicionarColisao(spriteChild);
            }
        }
    }

    private void AdicionarColisao(Sprite2D sprite)
    {
        // Cria um StaticBody2D para colisão
        var corpo = new StaticBody2D();
        sprite.AddChild(corpo);

        // Cria a forma de colisão
        var colisao = new CollisionShape2D();
        corpo.AddChild(colisao);

        // Define a forma como retângulo baseado na textura
        var forma = new RectangleShape2D();
        if (sprite.Texture != null)
        {
            forma.Size = sprite.Texture.GetSize();
            colisao.Shape = forma;
        }
        else
        {
            GD.Print($"Sprite '{sprite.Name}' não tem textura definida.");
        }
    }
}
