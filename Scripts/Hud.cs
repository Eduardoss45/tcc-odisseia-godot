using Godot;
using System;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
    private TextureRect lifeIcon;
    private List<Texture2D> lifeStages = new List<Texture2D>();

    public override void _Ready()
    {
        lifeIcon = GetNode<TextureRect>("MarginContainer/VBoxContainer/Life");

        for (int i = 0; i <= 9; i++)
        {
            var path = $"res://Sprites/hp/{i}.png";
            GD.Print("Carregando: ", path);
            var tex = GD.Load<Texture2D>(path);
            if (tex == null)
                GD.PrintErr("Erro ao carregar ", path);
            else
                lifeStages.Add(tex);
        }

        UpdateLife(1);
    }


    public void UpdateLife(int stage)
    {
        stage = Mathf.Clamp(stage, 0, lifeStages.Count - 1);
        lifeIcon.Texture = lifeStages[stage];
    }
}
